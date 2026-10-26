using Microsoft.AspNetCore.Mvc.ModelBinding;
using MvcLoveMoney.Services;

namespace MvcLoveMoney.Helpers
{
    // Binds an encrypted Id (made by IIdProtector) back to an int.
    // Use: [ModelBinder(typeof(ProtectedIdModelBinder))] on an "int id" / "int? id" parameter or an Id property.
    public class ProtectedIdModelBinder : IModelBinder
    {
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            var value = bindingContext.ValueProvider.GetValue(bindingContext.ModelName).FirstValue;

            // Nothing sent, or "0" (= a new record in the Create form) → keep the default value
            if (string.IsNullOrEmpty(value) || value == "0")
                return Task.CompletedTask;

            var idProtector = bindingContext.HttpContext.RequestServices.GetRequiredService<IIdProtector>();
            var id = idProtector.Unprotect(value);

            if (id == null)
            {
                // Plain numbers or tampered strings are rejected
                bindingContext.ModelState.TryAddModelError(bindingContext.ModelName, "Invalid id.");
                return Task.CompletedTask;
            }

            bindingContext.Result = ModelBindingResult.Success(id.Value);
            return Task.CompletedTask;
        }
    }
}
