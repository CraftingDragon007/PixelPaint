namespace PixelPaint.Services;

public interface IUserPreferencesStore
{
    UserPreferences Load();

    void Save(UserPreferences preferences);
}

