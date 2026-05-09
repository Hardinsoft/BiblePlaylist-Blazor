using BiblePlaylist.Client.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BiblePlaylist.Client.Pages
{
    public class BasePage : ComponentBase
    {      

        protected override async Task OnInitializedAsync()
        { 
            await InvokeAsync(StateHasChanged);
        }
        
       
    }
}
