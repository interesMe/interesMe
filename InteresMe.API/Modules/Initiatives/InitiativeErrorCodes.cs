namespace InteresMe.API.Modules.Initiatives;

public static class InitiativeErrorCodes
{
    public const string NotFound = "initiative.not_found";
    public const string UserNotFound = "initiative.user_not_found";
    public const string CreateValidationFailed = "initiative.create.validation_failed";
    public const string UpdateValidationFailed = "initiative.update.validation_failed";
    public const string UpdateForbidden = "initiative.update.forbidden";
    public const string DeleteForbidden = "initiative.delete.forbidden";
    public const string ApplyOwnerForbidden = "initiative.apply.owner_forbidden";
    public const string ApplyMessageTooLong = "initiative.apply.message_too_long";
    public const string ApplyRoleInvalid = "initiative.apply.role_invalid";
    public const string ApplyAlreadyExists = "initiative.apply.already_exists";
    public const string ApplicationForbidden = "initiative.application.forbidden";
    public const string ApplicationNotFound = "initiative.application.not_found";
    public const string ApplicationConflict = "initiative.application.conflict";
    public const string OwnerRequired = "initiative.owner_required";
}
