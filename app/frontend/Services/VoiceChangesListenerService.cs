// Copyright (c) Microsoft. All rights reserved.

using Microsoft.JSInterop;

namespace ClientApp.Services;

public sealed class VoiceChangesListenerService(
    ISpeechSynthesisService speechSynthesisService) : IVoiceChangesListener
{
    public void OnListenForVoiceChanges(Func<Task> onVoicesChanged) =>
        speechSynthesisService.OnVoicesChanged(onVoicesChanged);

    public void UnsubscribeFromVoiceChanges() =>
        speechSynthesisService.UnsubscribeFromVoicesChanged();
}
