namespace Wasl.API.Helper.CustomCssScalar
{
    public static class CssScalar
    {
        public static string GetCss()
        {
            return @"
                .scalar-card-content table, 
                 .markdown table {
            display: block !important;
            overflow-x: auto !important;
            white-space: nowrap !important;
            width: 100% !important;";
        }
    }
}