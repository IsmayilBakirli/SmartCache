namespace SmartCache.Application.Common.Enums
{
    public enum ResponseCode
    {
        Success = 200,
        BadRequest = 400,
        Unauthorized = 401,
        NotFound = 404,
        InternalServerError = 500,
        Created=201,
        Updated=204,
        Deleted=204,
        NotModified=304
    }
}
