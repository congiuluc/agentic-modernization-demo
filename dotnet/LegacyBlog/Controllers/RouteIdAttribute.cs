using System;
using System.Globalization;
using System.Web.Mvc;

namespace LegacyBlog.Controllers
{
    public sealed class RouteIdAttribute : CustomModelBinderAttribute
    {
        public override IModelBinder GetBinder() { return new RouteIdBinder(); }

        private sealed class RouteIdBinder : IModelBinder
        {
            public object BindModel(ControllerContext controller, ModelBindingContext binding)
            {
                object value;
                return controller.RouteData.Values.TryGetValue(binding.ModelName, out value)
                    ? (object)Convert.ToInt32(value, CultureInfo.InvariantCulture) : null;
            }
        }
    }
}
