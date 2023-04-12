// Copyright (c) Microsoft. All rights reserved.

namespace ClientApp.Components;

public sealed partial class SettingsPanel
{
    private readonly RequestOverrides _overrides = new();

    private Approach _approach = Approach.RetrieveThenRead;
    private bool _open;

    [Parameter]
#pragma warning disable BL0007 // Component parameters should be auto properties
    public bool Open
#pragma warning restore BL0007 // Component parameters should be auto properties
    {
        get => _open;
        set
        {
            if (_open == value)
            {
                return;
            }

            _open = value;
            OpenChanged.InvokeAsync(value);
        }
    }

    [Parameter] public EventCallback<bool> OpenChanged { get; set; }
}
