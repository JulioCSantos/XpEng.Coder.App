// Project: XpEng.Coder09.Models
namespace XpEng.Coder80.Infrastructure.Interfaces {
    public interface IConfigPersistence {
        string LoadConfigJson();
        void SaveConfigJson(string json);
    }
}