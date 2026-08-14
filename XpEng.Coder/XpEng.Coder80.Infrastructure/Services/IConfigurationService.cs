namespace XpEng.Coder80.Infrastructure.Services {
    public interface IConfigurationService {
        AppConfig Load();
        void Save(AppConfig config);
    }
}