

namespace Domain.Enums
{
    public enum UserRole
    {
        Customer = 0,
        Executor,
        Admin,
        SuperAdmin
    }

    public static class  UserRoleExtensions
    {
        public static string ToRoleName(this UserRole role) => role.ToString();
    }
}
