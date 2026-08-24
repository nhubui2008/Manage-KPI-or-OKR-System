FROM vllm/vllm-openai:v0.21.0@sha256:a230095847e93bd4df9888b33dab956fa9504537b828a23657d2b26fed57b5c9

LABEL org.opencontainers.image.source="https://github.com/opendatalab/MinerU" \
      org.opencontainers.image.version="0dfc9460cd9ab693b9af60ae3fbffd7bc111b062" \
      org.opencontainers.image.licenses="LicenseRef-MinerU-Open-Source-License"

USER root
RUN apt-get update \
    && apt-get install -y --no-install-recommends \
        curl \
        fontconfig \
        fonts-noto-cjk \
        fonts-noto-core \
        libgl1 \
    && fc-cache -f \
    && rm -rf /var/lib/apt/lists/* \
    && groupadd --gid 10001 mineru \
    && useradd --uid 10001 --gid 10001 --create-home --shell /usr/sbin/nologin mineru \
    && install -d -o mineru -g mineru /etc/mineru /var/lib/mineru/output

WORKDIR /opt/MinerU
COPY deploy/rag/mineru-requirements.lock /tmp/mineru-requirements.lock
COPY external/MinerU/ /opt/MinerU/
COPY --chown=10001:10001 deploy/rag/mineru.json /etc/mineru/mineru.json

# Install only hash-locked runtime dependencies. The pinned source is imported
# directly through PYTHONPATH, avoiding an unpinned PEP 517 build environment.
RUN python3 -m pip install --no-cache-dir --require-hashes \
        --extra-index-url https://download.pytorch.org/whl/cu130 \
        --break-system-packages -r /tmp/mineru-requirements.lock \
    && python3 -m pip cache purge \
    && python3 -c "from huggingface_hub import snapshot_download; snapshot_download('opendatalab/PDF-Extract-Kit-1.0', revision='ed6b654c018d742e65a17671e379c5e6ecc87ec9', local_dir='/opt/mineru-models')"

ENV MINERU_MODEL_SOURCE=local \
    MINERU_TOOLS_CONFIG_JSON=/etc/mineru/mineru.json \
    MINERU_API_MAX_CONCURRENT_REQUESTS=1 \
    MINERU_API_OUTPUT_ROOT=/var/lib/mineru/output \
    PYTHONPATH=/opt/MinerU \
    HOME=/home/mineru

USER 10001:10001

EXPOSE 8000
HEALTHCHECK --interval=30s --timeout=5s --start-period=15m --retries=5 \
    CMD curl --fail --silent http://127.0.0.1:8000/health >/dev/null || exit 1

ENTRYPOINT ["python3", "-m", "mineru.cli.fast_api"]
CMD ["--host", "0.0.0.0", "--port", "8000"]
