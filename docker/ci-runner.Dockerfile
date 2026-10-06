# The image every build runs in, both on the self-hosted runner and locally.
#
# runner/setup-runner.sh builds it on the build server and runner/in-container.sh runs each
# workflow step in it; docker/ci-local.sh builds the same tag on a developer machine. One image
# for both, so a local run and a pull request compile on the same base with the same SDK.
#
# Built on plain Ubuntu with the SDK installed by dotnet-install.sh. The
# mcr.microsoft.com/dotnet/sdk image would pin whatever SDK its tag carries rather than the one
# global.json asks for.
#
# Only what the pipeline invokes is installed, so a tool the image lacks cannot pass here and
# fail on a machine that has it.
#
# The build context is the repository root, narrowed by ci-runner.Dockerfile.dockerignore. The
# RemoteBuildTool is published into /opt/remotebuildtool here, once, so a pull request only runs it.

FROM ubuntu:24.04 AS sdk

# Read from global.json by whichever script builds the image, so it cannot drift from the
# repository.
ARG DOTNET_SDK_VERSION

SHELL ["/bin/bash", "-euo", "pipefail", "-c"]

RUN apt-get update \
    && apt-get install --yes --no-install-recommends \
        ca-certificates \
        curl \
        git \
        tzdata \
        libicu74 \
    && rm -rf /var/lib/apt/lists/*

RUN curl -fsSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh \
    && bash /tmp/dotnet-install.sh --version "${DOTNET_SDK_VERSION}" --install-dir /usr/share/dotnet \
    && ln -s /usr/share/dotnet/dotnet /usr/local/bin/dotnet \
    && rm /tmp/dotnet-install.sh

ENV DOTNET_ROOT=/usr/share/dotnet \
    PATH="/usr/share/dotnet:/root/.dotnet/tools:${PATH}" \
    DOTNET_NOLOGO=true \
    DOTNET_SKIP_FIRST_TIME_EXPERIENCE=true \
    DOTNET_CLI_TELEMETRY_OPTOUT=true

FROM sdk AS remotebuildtool

WORKDIR /src
COPY global.json nuget.config Directory.Build.props Directory.Build.targets Directory.Packages.props .editorconfig ./
COPY src/tools/Bitenovac.RemoteBuildTool.Core/ src/tools/Bitenovac.RemoteBuildTool.Core/
COPY src/tools/Bitenovac.RemoteBuildTool/ src/tools/Bitenovac.RemoteBuildTool/
RUN dotnet publish src/tools/Bitenovac.RemoteBuildTool -c Release -o /opt/remotebuildtool --nologo

FROM sdk

COPY --from=remotebuildtool /opt/remotebuildtool /opt/remotebuildtool

COPY docker/entrypoint.sh /usr/local/bin/entrypoint.sh
RUN chmod +x /usr/local/bin/entrypoint.sh

RUN mkdir -p /mnt/pr && chmod 1777 /mnt/pr

WORKDIR /repo
ENTRYPOINT ["/usr/local/bin/entrypoint.sh"]
