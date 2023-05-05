ARG VARIANT=bullseye
FROM --platform=amd64 mcr.microsoft.com/vscode/devcontainers/base:0-${VARIANT}
RUN export DEBIAN_FRONTEND=noninteractive \
    && apt-get update \
    && apt-get install -y xdg-utils \
    && curl -fsSL https://azuresdkreleasepreview.blob.core.windows.net/azd/standalone/pr/2052/install-azd.sh | bash -s -- --base-url https://azuresdkreleasepreview.blob.core.windows.net/azd/standalone/pr/2052 --version '' --verbose --skip-verify \
    && apt-get clean -y \
    && rm -rf /var/lib/apt/lists/*

