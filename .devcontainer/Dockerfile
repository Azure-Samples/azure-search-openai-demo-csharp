ARG IMAGE=bullseye
FROM --platform=amd64 mcr.microsoft.com/devcontainers/${IMAGE}
RUN apt-get update && \
    apt-get install -y --no-install-recommends xdg-utils && \
    rm -rf /var/lib/apt/lists/*
RUN export DEBIAN_FRONTEND=noninteractive && \
    curl -fsSL https://aka.ms/install-azd.sh | bash