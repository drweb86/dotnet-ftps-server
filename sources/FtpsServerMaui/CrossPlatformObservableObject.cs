using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace FtpsServerMaui;

public abstract class CrossPlatformObservableObject : ObservableObject
{
    protected bool SetPropertyPlatformAgnostic<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
#if WINDOWS
        return SetProperty(ref field, value, propertyName);
#else
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;

        field = value;
        OnPropertyChanged(propertyName);
        return true;
#endif
    }
}