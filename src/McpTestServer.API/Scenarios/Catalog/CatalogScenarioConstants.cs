namespace McpTestServer.API.Scenarios.Catalog;

public static class CatalogFacetNames
{
    public const string Resources = "resources";

    public const string Prompts = "prompts";
}

public static class CatalogPaginationFaults
{
    public const string None = "none";

    public const string CursorLoop = "cursor_loop";

    public const string DuplicatePage = "duplicate_page";

    public const string ErrorAfterPageN = "error_after_page_n";
}

public static class CatalogMutationActions
{
    public const string Add = "add";

    public const string Remove = "remove";

    public const string Rename = "rename";
}
