using System.Data;
using System.Text;
using Dapper;
using ViabilityIQ.Application.Interfaces;
using ViabilityIQ.Shared.DataModels;

namespace ViabilityIQ.Infrastructure.Repositories;

public sealed class OperationalReferenceService : IOperationalReferenceService
{
    private const int MaximumCodeLength = 12;
    private readonly ISessionService _sessionService;

    public OperationalReferenceService(ISessionService sessionService)
    {
        _sessionService = sessionService;
    }

    public async Task PrepareBusinessCodeAsync(
        Business business,
        IDbConnection connection,
        IDbTransaction transaction)
    {
        if (business.BusinessId > 0)
        {
            var existingCode = await connection.QuerySingleOrDefaultAsync<string?>(
                """
                SELECT BusinessCode
                FROM tblBusiness
                WHERE BusinessId = @BusinessId;
                """,
                new { business.BusinessId },
                transaction);
            if (!string.IsNullOrWhiteSpace(existingCode))
            {
                business.BusinessCode = existingCode;
                return;
            }
        }

        business.BusinessCode = await AllocateCodeAsync(
            business.BusinessName,
            "tblBusiness",
            "BusinessCode",
            "BusinessId",
            business.BusinessId,
            tenantId: null,
            connection,
            transaction);
    }

    public async Task PrepareBranchCodeAsync(
        Branch branch,
        IDbConnection connection,
        IDbTransaction transaction)
    {
        if (branch.BranchId > 0)
        {
            var existingCode = await connection.QuerySingleOrDefaultAsync<string?>(
                """
                SELECT BranchCode
                FROM tblBranch
                WHERE BranchId = @BranchId;
                """,
                new { branch.BranchId },
                transaction);
            if (!string.IsNullOrWhiteSpace(existingCode))
            {
                branch.BranchCode = existingCode;
                return;
            }
        }

        branch.BranchCode = await AllocateCodeAsync(
            branch.BranchName,
            "tblBranch",
            "BranchCode",
            "BranchId",
            branch.BranchId,
            _sessionService.TenantId,
            connection,
            transaction);
    }

    public async Task PrepareAssessmentAsync(
        Assessment assessment,
        bool isCreate,
        IDbConnection connection,
        IDbTransaction transaction)
    {
        if (!isCreate)
        {
            var issuedReference = await connection.QuerySingleOrDefaultAsync<AssessmentReferenceSnapshot>(
                """
                SELECT BranchId, CaseYear, CaseSequence, CaseNumber
                FROM tblAssessments
                WHERE AssessmentId = @AssessmentId;
                """,
                new { assessment.AssessmentId },
                transaction);
            if (issuedReference is not null)
            {
                assessment.BranchId = issuedReference.BranchId;
                assessment.CaseYear = issuedReference.CaseYear;
                assessment.CaseSequence = issuedReference.CaseSequence;
                assessment.CaseNumber = issuedReference.CaseNumber;
            }

            return;
        }

        var branchId = await connection.QuerySingleOrDefaultAsync<long?>(
            """
            SELECT membership.DefaultBranchId
            FROM tblTenantMembership membership
            INNER JOIN tblBranch branch
                ON branch.BranchId = membership.DefaultBranchId
               AND branch.TenantId = membership.TenantId
               AND branch.Active = 1
            WHERE membership.TenantMembershipId = @MembershipId
              AND membership.TenantId = @TenantId
              AND membership.UserId = @UserId
              AND membership.Active = 1
              AND membership.MembershipStatus = 'Active';
            """,
            new
            {
                MembershipId = _sessionService.TenantMembershipId,
                TenantId = _sessionService.TenantId,
                UserId = _sessionService.UserId
            },
            transaction);
        if (!branchId.HasValue || branchId.Value <= 0)
        {
            throw new InvalidOperationException(
                "Your active tenant membership does not have an active Site assignment. Contact your tenant administrator.");
        }

        assessment.BranchId = branchId.Value;
        assessment.CaseYear = assessment.CreatedDate.Year;
        assessment.CaseSequence = 0;
        assessment.CaseNumber = null;
    }

    public async Task FinalizeAssessmentCaseNumberAsync(
        Assessment assessment,
        long assessmentId,
        IDbConnection connection,
        IDbTransaction transaction)
    {
        var reference = await connection.QuerySingleOrDefaultAsync<CaseReferenceParts>(
            """
            SELECT
                business.BusinessCode,
                branch.BranchCode
            FROM tblAssessments assessment
            INNER JOIN tblBusiness business
                ON business.BusinessId = assessment.BusinessId
               AND business.TenantId = assessment.TenantId
            INNER JOIN tblBranch branch
                ON branch.BranchId = assessment.BranchId
               AND branch.TenantId = assessment.TenantId
            WHERE assessment.AssessmentId = @AssessmentId
              AND assessment.TenantId = @TenantId;
            """,
            new
            {
                AssessmentId = assessmentId,
                TenantId = _sessionService.TenantId
            },
            transaction);

        if (reference is null
            || string.IsNullOrWhiteSpace(reference.BusinessCode)
            || string.IsNullOrWhiteSpace(reference.BranchCode))
        {
            throw new InvalidOperationException(
                "The selected Business and Site must have generated codes before an assessment can be created.");
        }

        var caseYear = assessment.CreatedDate.Year;
        var lockResource =
            $"ViabilityIQ:Case:{assessment.BranchId}:{caseYear}";
        var lockResult = await connection.ExecuteScalarAsync<int>(
            """
            DECLARE @Result int;
            EXEC @Result = sys.sp_getapplock
                @Resource = @Resource,
                @LockMode = 'Exclusive',
                @LockOwner = 'Transaction',
                @LockTimeout = 15000;
            SELECT @Result;
            """,
            new { Resource = lockResource },
            transaction);
        if (lockResult < 0)
        {
            throw new TimeoutException(
                "The Site case-number sequence is busy. Please retry the assessment save.");
        }

        var sequence = await connection.ExecuteScalarAsync<int>(
            """
            DECLARE @NextSequence int;

            UPDATE dbo.tblAssessmentCaseSequence WITH (UPDLOCK, HOLDLOCK)
            SET @NextSequence = LastSequence = LastSequence + 1,
                ModifiedDate = SYSUTCDATETIME()
            WHERE BranchId = @BranchId
              AND CaseYear = @CaseYear;

            IF @@ROWCOUNT = 0
            BEGIN
                SET @NextSequence = 1;
                INSERT INTO dbo.tblAssessmentCaseSequence
                    (BranchId, CaseYear, LastSequence, ModifiedDate)
                VALUES
                    (@BranchId, @CaseYear, @NextSequence, SYSUTCDATETIME());
            END;

            SELECT @NextSequence;
            """,
            new
            {
                assessment.BranchId,
                CaseYear = caseYear
            },
            transaction);

        var caseNumber =
            $"VIQ-{reference.BranchCode}-{reference.BusinessCode}-{caseYear % 100:D2}/{sequence:D5}";
        await connection.ExecuteAsync(
            """
            UPDATE dbo.tblAssessments
            SET CaseNumber = @CaseNumber,
                CaseYear = @CaseYear,
                CaseSequence = @CaseSequence
            WHERE AssessmentId = @AssessmentId;
            """,
            new
            {
                CaseNumber = caseNumber,
                CaseYear = caseYear,
                CaseSequence = sequence,
                AssessmentId = assessmentId
            },
            transaction);

        assessment.AssessmentId = assessmentId;
        assessment.CaseNumber = caseNumber;
        assessment.CaseYear = caseYear;
        assessment.CaseSequence = sequence;
    }

    private static async Task<string> AllocateCodeAsync(
        string? name,
        string table,
        string codeColumn,
        string idColumn,
        long currentId,
        long? tenantId,
        IDbConnection connection,
        IDbTransaction transaction)
    {
        var candidates = BuildThreeLetterCandidates(name);
        foreach (var candidate in candidates)
        {
            if (await IsAvailableAsync(
                    candidate,
                    table,
                    codeColumn,
                    idColumn,
                    currentId,
                    tenantId,
                    connection,
                    transaction))
            {
                return candidate;
            }
        }

        var baseCode = candidates[0];
        for (var suffix = 0; suffix < int.MaxValue; suffix++)
        {
            var suffixText = suffix.ToString();
            var prefixLength = Math.Min(
                baseCode.Length,
                MaximumCodeLength - suffixText.Length);
            var candidate = $"{baseCode[..prefixLength]}{suffixText}";
            if (await IsAvailableAsync(
                    candidate,
                    table,
                    codeColumn,
                    idColumn,
                    currentId,
                    tenantId,
                    connection,
                    transaction))
            {
                return candidate;
            }
        }

        throw new InvalidOperationException("No available operational code could be generated.");
    }

    private static async Task<bool> IsAvailableAsync(
        string candidate,
        string table,
        string codeColumn,
        string idColumn,
        long currentId,
        long? tenantId,
        IDbConnection connection,
        IDbTransaction transaction)
    {
        var tenantPredicate = tenantId.HasValue
            ? "AND TenantId = @TenantId"
            : string.Empty;
        var count = await connection.ExecuteScalarAsync<long>(
            $"""
            SELECT COUNT_BIG(1)
            FROM dbo.[{table}] WITH (UPDLOCK, HOLDLOCK)
            WHERE [{codeColumn}] = @Candidate
              AND [{idColumn}] <> @CurrentId
              {tenantPredicate};
            """,
            new
            {
                Candidate = candidate,
                CurrentId = currentId,
                TenantId = tenantId
            },
            transaction);
        return count == 0;
    }

    internal static IReadOnlyList<string> BuildThreeLetterCandidates(string? name)
    {
        var words = (name ?? string.Empty)
            .ToUpperInvariant()
            .Split([' ', '-', '_', '.', ',', '/', '\\', '(', ')', '&'],
                StringSplitOptions.RemoveEmptyEntries)
            .Select(OnlyLettersAndDigits)
            .Where(word => word.Length > 0)
            .Where(word => !LegalSuffixes.Contains(word))
            .ToArray();
        var joined = string.Concat(words);
        if (joined.Length < 3)
        {
            throw new InvalidOperationException(
                "The name must contain at least three letters or numbers to generate a code.");
        }

        var candidates = new List<string>();
        if (words.Length >= 3)
        {
            AddCandidate(candidates, string.Concat(words.Take(3).Select(word => word[0])));
        }
        else if (words.Length == 2)
        {
            if (words[1].Length > 1)
            {
                AddCandidate(candidates, $"{words[0][0]}{words[1][0]}{words[1][1]}");
            }
            if (words[0].Length > 1)
            {
                AddCandidate(candidates, $"{words[0][0]}{words[0][1]}{words[1][0]}");
            }
        }

        AddCandidate(candidates, joined[..3]);
        for (var second = 1; second < joined.Length - 1; second++)
        {
            for (var third = second + 1; third < joined.Length; third++)
            {
                AddCandidate(candidates, $"{joined[0]}{joined[second]}{joined[third]}");
            }
        }

        return candidates;
    }

    private static string OnlyLettersAndDigits(string value)
    {
        var builder = new StringBuilder(value.Length);
        foreach (var character in value)
        {
            if (char.IsAsciiLetterOrDigit(character))
            {
                builder.Append(character);
            }
        }

        return builder.ToString();
    }

    private static void AddCandidate(ICollection<string> candidates, string candidate)
    {
        if (candidate.Length == 3 && !candidates.Contains(candidate))
        {
            candidates.Add(candidate);
        }
    }

    private static readonly HashSet<string> LegalSuffixes =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "PTY", "LTD", "LIMITED", "CC", "INC", "CORP", "CORPORATION", "LLC"
        };

    private sealed class AssessmentReferenceSnapshot
    {
        public long BranchId { get; init; }
        public int CaseYear { get; init; }
        public int CaseSequence { get; init; }
        public string? CaseNumber { get; init; }
    }

    private sealed class CaseReferenceParts
    {
        public string? BusinessCode { get; init; }
        public string? BranchCode { get; init; }
    }
}
