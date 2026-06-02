using Microsoft.AspNetCore.Components;
using System.Reflection;

namespace BrucolWeb.Web.Components.Common
{
    public partial class ZDataTableComponent<TItem> : ComponentBase
    {
        [Parameter] public IEnumerable<TItem>? Items { get; set; }
        [Parameter] public EventCallback<TItem> OnEdit { get; set; }

        protected List<PropertyInfo> Properties = new();

        protected override void OnParametersSet()
        {
            if (Items != null && Items.Any())
            {
                Properties = typeof(TItem).GetProperties().ToList();
            }
        }
    }
}
