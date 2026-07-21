using HabitFlow.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace HabitFlow.Web.ViewComponents;

public sealed class MNSOFTLogoViewComponent : ViewComponent
{
    private const string OfficialLogoPath = "/brand/mnsoft/logo-mnsoft-oficial.png";
    private readonly IBrandAssetService brandAssetService;

    public MNSOFTLogoViewComponent(IBrandAssetService brandAssetService) => this.brandAssetService = brandAssetService;

    public IViewComponentResult Invoke(string context = "footer")
    {
        return View(new MNSOFTLogoViewModel(OfficialLogoPath, brandAssetService.Exists(OfficialLogoPath), context));
    }
}

public sealed record MNSOFTLogoViewModel(string OfficialLogoPath, bool HasOfficialLogo, string Context);
