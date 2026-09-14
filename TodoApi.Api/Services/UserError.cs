namespace TodoApi.Api.Services;

public enum UserError
{
    None,
    NotFound,
    EmailTaken,
    /// <summary>An admin tried to delete their own account or change their own role.</summary>
    SelfModification,
}
