# Local RAG provider services

This deployment uses [MinerU](https://github.com/opendatalab/MinerU) under the
MinerU Open Source License (Apache 2.0 plus its additional commercial-threshold
and online-service attribution terms). The source is pinned as the
`external/MinerU` Git submodule at source tag `mineru-3.4.4-released`, commit
`0dfc9460cd9ab693b9af60ae3fbffd7bc111b062`. Source at that commit reports
runtime version `3.4.3`; the adapter validates that exact provider contract
instead of inferring a runtime version from the tag name.

Initialize the pinned source after cloning this application:

```bash
git submodule update --init --depth 1 external/MinerU
```

Start only ClamAV:

```bash
docker compose -f deploy/rag/compose.local.yml up -d clamav
printf 'zPING\0' | nc -w 3 127.0.0.1 3310
```

Build and start MinerU's single-worker `pipeline` backend:

```bash
docker compose -f deploy/rag/compose.local.yml --profile mineru build mineru
docker compose -f deploy/rag/compose.local.yml --profile mineru up -d mineru
curl --fail http://127.0.0.1:8000/health

# Synchronous smoke test; replace sample.pdf with a small non-sensitive file.
curl --fail -F 'files=@sample.pdf;type=application/pdf' \
  -F 'backend=pipeline' -F 'return_md=true' \
  http://127.0.0.1:8000/file_parse
```

Start the pinned BGE-M3 model with Hugging Face Text Embeddings Inference:

```bash
docker compose -f deploy/rag/compose.local.yml --profile bge up -d bge-m3
curl --fail http://127.0.0.1:8080/health
curl --fail http://127.0.0.1:8080/v1/embeddings \
  -H 'Content-Type: application/json' \
  -d '{"model":"BAAI/bge-m3","input":"KPI tăng trưởng doanh thu"}'
```

The TEI image is pinned for NVIDIA compute capability 8.6 and the model is
pinned to revision `5617a9f61b028005a4858fdac845db406aefb181`. Configure the
application with:

```dotenv
BgeM3__Endpoint=http://127.0.0.1:8080/v1/embeddings
BgeM3__Model=BAAI/bge-m3
BgeM3__Dimensions=1024
```

The image installs hash-locked Python dependencies, imports the pinned checkout
directly through `PYTHONPATH`, uses the checked-in MinerU configuration, and
downloads the pipeline model at revision
`ed6b654c018d742e65a17671e379c5e6ecc87ec9`. It binds the API to loopback and
limits processing to one request at a time. Configure the application with:

```dotenv
MinerU__Endpoint=http://127.0.0.1:8000/file_parse
MinerU__ApiKey=
MinerU__TimeoutSeconds=3600
MalwareScanner__Host=127.0.0.1
MalwareScanner__Port=3310
DocumentIngestion__PipelineVersion=mineru-0dfc9460-pipeline-ed6b654c|bge-m3-5617a9f6-tei-939883b2|azure-schema-v1
```

MinerU documents a minimum of 16 GB system RAM and 4 GB VRAM for the pipeline
backend. Do not start the local service on a smaller machine; use a private
remote deployment behind HTTPS instead. Keep port 8000 loopback-only because
the upstream API has no built-in bearer authentication. The BGE service also
requires NVIDIA Container Toolkit; both provider ports remain loopback-only.
The provided MinerU container runs as UID/GID 10001, drops Linux capabilities,
sets `no-new-privileges`, and uses private shared memory. Process untrusted
documents on a dedicated host or VM; do not colocate this parser with unrelated
sensitive workloads.

The worker calls synchronous `/file_parse` under its SQL lease heartbeat.
MinerU may compute again after a timeout or lost connection, while the private
Blob result and Azure Search records converge on deterministic ingestion-intent
keys. This is at-least-once compute with convergent persisted state, not an
exactly-once promise from the upstream process-local task manager.
