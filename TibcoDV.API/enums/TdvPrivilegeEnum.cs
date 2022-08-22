namespace NoP77svk.TibcoDV.API
{
    public enum TdvPrivilegeEnum
    {
        Read = 1,
        Write = 2,
        Execute = 4,
        Select = 8,
        Insert = 16,
        Update = 32,
        Delete = 64,
        Grant = 128
    }
}
