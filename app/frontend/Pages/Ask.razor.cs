// Copyright (c) Microsoft. All rights reserved.

namespace ClientApp.Pages;

public sealed partial class Ask
{
    private string _userPrompt = "";
    private bool _isReceivingResponse = false;
    private string? _intermediateResponse = null;
}
