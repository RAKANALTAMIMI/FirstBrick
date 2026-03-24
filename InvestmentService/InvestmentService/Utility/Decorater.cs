using System.Data.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

public class DbExceptionFilter : ExceptionFilterAttribute
{
    public override void OnException(ExceptionContext context)
    {
        if (context.Exception is DbUpdateException || context.Exception is DbException)
        {
            context.Result = new ObjectResult("Database error occurred.")
            {
                StatusCode = 500
            };
            context.ExceptionHandled = true;
        }
    }
}