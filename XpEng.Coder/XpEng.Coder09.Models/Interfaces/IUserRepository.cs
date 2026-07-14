using XpEng.Coder09.Models.Entities;

namespace XpEng.Coder09.Models.Interfaces;

public interface IUserRepository {
    // The contract defines operations, not properties
    User? GetUserById(int id);
    void AddUser(User user);
}