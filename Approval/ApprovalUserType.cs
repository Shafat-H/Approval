namespace SME.Helper.Approval
{
    public static class ApprovalUserType
    {
        public static int User { get; } = 1;
        public static int UserGroup { get; } = 2;
        public static int LineManager { get; } = 3;
        public static int Supervisor { get; } = 4;
    }
    public static class ApprovalUserTypeName
    {
        public static string User { get; } = "User";
        public static string UserGroup { get; } = "User Group";
        public static string LineManager { get; } = "Line Manager";
        public static string Supervisor { get; } = "Supervisor";
    }
}