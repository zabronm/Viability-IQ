using Microsoft.AspNetCore.Components;
using ViabilityIQ.Shared.DataModels.SecurityDataModels;

namespace ViabilityIQ.Web.Components.Pages.HomePageComponents
{
    public partial class GreetingSectionComponent : ComponentBase
    {
        [Parameter] public ApplicationUser CurrentUser { get; set; }
        [Parameter] public DateTime LastLoginDate { get; set; }
        [Parameter] public string CurrentBranch { get; set; }


        /// Get initials from user's first and last name for avatar        
        /// <returns>Two-character initials or "?" if names missing</returns>
        private string GetInitials()
        {
            if (CurrentUser == null) return "?";
            var first = CurrentUser.FirstName?.FirstOrDefault() ?? '?';
            var last = CurrentUser.LastName?.FirstOrDefault() ?? '?';
            return $"{first}{last}".ToUpper();
        }


        /// Format the last login date using relative format for recent dates,
        /// absolute format for older dates (following dashboard specification)

        /// <param name="date">The last login date</param>
        /// <returns>Formatted date string</returns>
        private string FormatLastLoginDate(DateTime date)
        {
            var now = DateTime.UtcNow;
            var diff = now - date;

            // Less than 1 hour: show minutes
            if (diff.TotalHours < 1)
                return $"{(int)diff.TotalMinutes} minutes ago";

            // Less than 24 hours: show hours
            else if (diff.TotalHours < 24)
                return $"{(int)diff.TotalHours} hour{((int)diff.TotalHours > 1 ? "s" : "")} ago";

            // Less than 1 week: show days
            else if (diff.TotalDays < 7)
                return $"{(int)diff.TotalDays} day{((int)diff.TotalDays > 1 ? "s" : "")} ago";

            // 1 week or more: show absolute date
            else
                return date.ToString("MMM d, h:mm tt");
        }
    }
}

