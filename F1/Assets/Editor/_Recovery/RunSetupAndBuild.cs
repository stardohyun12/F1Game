public static class RunSetupAndBuild
{
    public static void Execute()
    {
        SetupLayoutAssets.Run();
        BuildFromLayoutMenu.Build();
    }
}
