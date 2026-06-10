using DryIoc;
using DOTS.Adapters;

namespace DOTS.Features.CultureConversion;

public static class CultureConversionIoC
{
    public static void RegisterCultureConversionFeature(IContainer container)
    {
        container.Register<ICultureConversionConfigProvider, CultureConversionConfigProvider>(Reuse.Singleton);
        container.Register<ICultureConversionSettingsProvider, CultureConversionSettingsProvider>(Reuse.Singleton);
        container.Register<ICultureConversionStore, CultureConversionStore>(Reuse.Singleton);
        container.Register<ICultureConversionAdapter, CultureConversionAdapter>(Reuse.Singleton);
        container.Register<ICultureConversionService, CultureConversionService>(Reuse.Singleton);
    }
}
