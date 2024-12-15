using System.Windows.Controls;

namespace CRUDTableOperations.Contracts.Services;

public interface IPageService
{
    Type GetPageType(string key);

    Page GetPage(string key);
}
