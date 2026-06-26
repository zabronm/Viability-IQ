namespace ViabilityIQ.Shared.SharedModels
{
    public enum PageType
    {
        Banks,
        Dashboards,
        Assessments,
        Assessment,
        Businesses,
        Contacts,
        Communication,
        Settings,
        Help,
        Users,
        UserGroups,
        Account,
        ActivityLog,
        Suggestions,
        Loans,
        Debtors,
        Creditors,
        Reports,
        ProductCategory,
        Product,
        Province,
        Company,
        Sectors,
        Expenses,
        Stock,
        VAT,
        Sales
    }

    public static class PageHeaderIcons
    {
        /// <summary>
        /// Global translation dictionary matching a PageType enum to its respective Bootstrap Icon class string.
        /// </summary>
        public static string GetIconClass(PageType pageType) => pageType switch
        {
            PageType.Dashboards => "bi bi-grid",
            PageType.Settings => "bi bi-gear",
            PageType.Users => "bi bi-people-fill",
            PageType.Assessments => "bi bi-stack",
            PageType.Assessment => "bi bi-pin-angle-fill",
            PageType.Account => "bi bi-person-vcard-fill",
            PageType.Communication => "bi bi-bell-fill",
            PageType.Businesses => "bi bi-buildings",
            PageType.Contacts => "bi bi-person-lines-fill",
            PageType.Help => "bi bi-question-circle-fill",
            PageType.Suggestions => "bi bi-person-raised-hand",
            PageType.Loans => "bi bi-piggy-bank",
            PageType.Creditors => "bi bi-incognito",
            PageType.Debtors => "bi bi-briefcase",
            PageType.ActivityLog => "bi bi-calendar3",
            PageType.UserGroups => "bi bi-people",
            PageType.Reports => "bi bi-clipboard2-data",
            PageType.Banks => "bi bi-bank",
            PageType.Product => "bi bi-box-seam-fill",
            PageType.ProductCategory => "bi bi-diagram-3-fill",
            PageType.Company => "bi bi-building-gear",
            PageType.Province => "bi bi-geo-alt-fill",
            PageType.Sectors => "bi bi-inboxes-fill",
            PageType.Sales => "bi bi-graph-up-arrow",
            PageType.Expenses => "bi bi-wallet2",
            PageType.Stock => "bi bi-boxes",
            PageType.VAT => "bi bi-bug",

            _ => "bi bi-circle"
        };
    }
}