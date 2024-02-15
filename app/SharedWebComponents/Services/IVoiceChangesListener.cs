// Copyright (c) Microsoft. All rights reserved.

namespace SharedWebComponents.Services;

public interface IVoiceChangesListener
{
    void OnListenForVoiceChanges(Func<Task> onVoicesChanged);

    void UnsubscribeFromVoiceChanges();
}
