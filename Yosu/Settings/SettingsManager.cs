﻿using System;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;
using Yosu.Settings.Encryption;
using Yosu.Settings.Serialization;

namespace Yosu.Settings;

/// <summary>
/// Derive from this class to create a custom settings manager that can de-/serialize its public properties from/to file
/// </summary>
public abstract partial class SettingsManager
{
    private bool _isSaved = true;

    /// <summary>
    /// Configuration for this <see cref="SettingsManager"/> instance
    /// </summary>
    [Ignore]
    public Configuration Configuration { get; set; }

    /// <summary>
    /// Full path of the settings file
    /// </summary>
    [Ignore]
    public IDataProtector? DataProtector { get; set; } = default;

    /// <summary>
    /// Whether the settings have been saved since the last time they were changed
    /// </summary>
    [Ignore]
    public bool IsSaved
    {
        get => _isSaved;
        protected set => Set(ref _isSaved, value);
    }

    /// <summary>
    /// Creates a settings manager
    /// </summary>
    protected SettingsManager()
    {
        // Wire to its own INotifyPropertyChanged to handle IsSaved
        PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName != nameof(IsSaved))
                IsSaved = false;
        };

        // Set default configuration
        Configuration = new Configuration
        {
            Key = GetType().Name
        };
    }

    /// <summary>
    /// Copies values of accessable properties from the given settings manager into the current
    /// </summary>
    public virtual void CopyFrom(SettingsManager referenceSettingsManager)
    {
        if (referenceSettingsManager == null)
            throw new ArgumentNullException(nameof(referenceSettingsManager));

        var serialized = Serializer.Serialize(referenceSettingsManager);
        Serializer.Populate(serialized, this);

        IsSaved = referenceSettingsManager.IsSaved;
    }

    /// <summary>
    /// Saves the settings to cache
    /// </summary>
    public virtual void Save()
    {
        SaveAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Saves the settings to cache
    /// </summary>
    public virtual async Task SaveAsync()
    {
        try
        {
            // Write file
            var data = Serializer.Serialize(this);
            if (DataProtector is not null)
                data = DataProtector.Protect(data);

            if (Configuration.UseSecureStorage)
                await SecureStorage.SetAsync(Configuration.Key, data);
            else
                Preferences.Set(Configuration.Key, data);

            IsSaved = true;
        }
        catch
        {
            if (Configuration.ThrowIfCannotSave)
                throw;
        }
    }

    /// <summary>
    /// Loads settings from cache
    /// </summary>
    public virtual void Load()
    {
        LoadAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Loads settings from cache
    /// </summary>
    public virtual async Task LoadAsync()
    {
        try
        {
            var data = Configuration.UseSecureStorage ?
                await SecureStorage.GetAsync(Configuration.Key) : Preferences.Get(Configuration.Key, null);

            if (data is null) return;

            if (DataProtector is not null)
                data = DataProtector.Unprotect(data);

            Serializer.Populate(data, this);

            IsSaved = true;
        }
        catch
        {
            if (Configuration.ThrowIfCannotLoad)
                throw;
        }
    }

    /// <summary>
    /// Resets settings back to default values
    /// </summary>
    public virtual void Reset()
    {
        var referenceSettings = (SettingsManager)Activator.CreateInstance(GetType())!;
        CopyFrom(referenceSettings);
        IsSaved = false;
    }

    /// <summary>
    /// Deletes the settings cache
    /// </summary>
    public virtual void Delete()
    {
        if (Configuration.UseSecureStorage)
            SecureStorage.Remove(Configuration.Key);
        else
            Preferences.Remove(Configuration.Key);
    }
}