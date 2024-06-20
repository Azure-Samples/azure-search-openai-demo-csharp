// Copyright (c) Microsoft. All rights reserved.

using Microsoft.JSInterop;

namespace ClientApp.Services;

public sealed class TextToSpeechPreferencesListenerService(
    ISpeechSynthesisService speechSynthesisService) : ITextToSpeechPreferencesListener
{
    public void OnAvailableVoicesChanged(Func<Task> onVoicesChanged) =>
        speechSynthesisService.OnVoicesChanged(onVoicesChanged);

    public void UnsubscribeFromAvailableVoicesChanged() =>
        speechSynthesisService.UnsubscribeFromVoicesChanged();
}
