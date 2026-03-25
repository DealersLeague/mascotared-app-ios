using System.Linq;
using TaskPopupPage = Mascotared.Views.TaskPopup;// ← namespace confirmado
using Favoritos=Mascotared.Views.Favoritos;


namespace Mascotared;

public static class NavigationHelper
{
    public static async Task NavigateToMainPage(INavigation navigation)
    {
        // Si ya estamos en MainPage, no navegar
        var currentPage = navigation.NavigationStack.LastOrDefault();
        if (currentPage is MainPage)
            return;

        // Si MainPage está en el stack, hacer Pop hasta llegar a ella
        if (navigation.NavigationStack.Any(p => p is MainPage))
        {
            while (navigation.NavigationStack.Count > 1 && !(navigation.NavigationStack.LastOrDefault() is MainPage))
            {
                await navigation.PopAsync();
            }
        }
        else
        {
            // Si no está en el stack, navegar a ella
            await navigation.PushAsync(new MainPage(), animated: true);
        }
    }

    public static async Task NavigateToFavoritos(INavigation navigation)
    {
        // Si ya estamos en Favoritos, no navegar
        var currentPage = navigation.NavigationStack.LastOrDefault();
        if (currentPage is Favoritos)
            return;

        // Si Favoritos está en el stack, hacer Pop hasta llegar a ella
        if (navigation.NavigationStack.Any(p => p is Favoritos))
        {
            while (navigation.NavigationStack.Count > 1 && !(navigation.NavigationStack.LastOrDefault() is Favoritos))
            {
                await navigation.PopAsync();
            }
        }
        else
        {
            await navigation.PushAsync(new Favoritos(), animated: true);
        }
    }

    public static async Task NavigateToSolicitudes(INavigation navigation)
    {
        var currentPage = navigation.NavigationStack.LastOrDefault();
        if (currentPage is TaskPopupPage)
            return;
        if (navigation.NavigationStack.Any(p => p is TaskPopupPage))
        {
            while (navigation.NavigationStack.Count > 1 &&
                   !(navigation.NavigationStack.LastOrDefault() is TaskPopupPage))
            {
                await navigation.PopAsync();
            }
        }
        else
        {
            await navigation.PushAsync(new TaskPopupPage(), animated: true);
        }
    }

    public static async Task NavigateToMessages(INavigation navigation)
    {
        // Si ya estamos en Messages, no navegar
        var currentPage = navigation.NavigationStack.LastOrDefault();
        if (currentPage is Messages)
            return;

        // Si Messages está en el stack, hacer Pop hasta llegar a ella
        if (navigation.NavigationStack.Any(p => p is Messages))
        {
            while (navigation.NavigationStack.Count > 1 && !(navigation.NavigationStack.LastOrDefault() is Messages))
            {
                await navigation.PopAsync();
            }
        }
        else
        {
            await navigation.PushAsync(new Messages(), animated: true);
        }
    }

    public static async Task NavigateToPerfil(INavigation navigation)
    {
        // Si ya estamos en PerfilConfigUser, no navegar
        var currentPage = navigation.NavigationStack.LastOrDefault();
        if (currentPage is PerfilConfigUser)
            return;

        // Si PerfilConfigUser está en el stack, hacer Pop hasta llegar a ella
        if (navigation.NavigationStack.Any(p => p is PerfilConfigUser))
        {
            while (navigation.NavigationStack.Count > 1 && !(navigation.NavigationStack.LastOrDefault() is PerfilConfigUser))
            {
                await navigation.PopAsync();
            }
        }
        else
        {
            await navigation.PushAsync(new PerfilConfigUser(), animated: true);
        }
    }
    public static async Task NavigateToProfile(INavigation navigation)
    {
        await navigation.PushAsync(new PerfilConfigUser());
    }
}
