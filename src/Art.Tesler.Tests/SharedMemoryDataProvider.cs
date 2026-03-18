using System.CommandLine;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Art.Common.Management;

namespace Art.Tesler.Tests;

internal class SharedMemoryDataProvider : ITeslerDataProvider
{
    private readonly InMemoryArtifactDataManager _artifactDataManager = new();

    public void Initialize(Command command)
    {
    }

    public IArtifactDataManager CreateArtifactDataManager(ParseResult parseResult)
    {
        return new PersistentProxyArtifactDataManager(_artifactDataManager);
    }

    private class PersistentProxyArtifactDataManager : IArtifactDataManager
    {
        private readonly IArtifactDataManager _artifactDataManager;
        private bool _disposed;

        public PersistentProxyArtifactDataManager(IArtifactDataManager artifactDataManager)
        {
            _artifactDataManager = artifactDataManager;
        }

        public ValueTask<CommittableStream> CreateOutputStreamAsync(ArtifactResourceKey key, OutputStreamOptions? options = null, CancellationToken cancellationToken = default)
        {
            EnsureNotDisposed();
            return _artifactDataManager.CreateOutputStreamAsync(key, options, cancellationToken);
        }

        public ValueTask<bool> ExistsAsync(ArtifactResourceKey key, CancellationToken cancellationToken = default)
        {
            EnsureNotDisposed();
            return _artifactDataManager.ExistsAsync(key, cancellationToken);
        }

        public ValueTask<bool> DeleteAsync(ArtifactResourceKey key, CancellationToken cancellationToken = default)
        {
            EnsureNotDisposed();
            return _artifactDataManager.DeleteAsync(key, cancellationToken);
        }

        public ValueTask<Stream> OpenInputStreamAsync(ArtifactResourceKey key, CancellationToken cancellationToken = default)
        {
            EnsureNotDisposed();
            return _artifactDataManager.OpenInputStreamAsync(key, cancellationToken);
        }

        public ValueTask OutputMemoryAsync(ReadOnlyMemory<byte> buffer, ArtifactResourceKey key, OutputStreamOptions? options = null, CancellationToken cancellationToken = default)
        {
            return _artifactDataManager.OutputMemoryAsync(buffer, key, options, cancellationToken);
        }

        public ValueTask OutputTextAsync(string text, ArtifactResourceKey key, OutputStreamOptions? options = null, CancellationToken cancellationToken = default)
        {
            EnsureNotDisposed();
            return _artifactDataManager.OutputTextAsync(text, key, options, cancellationToken);
        }

        public ValueTask OutputJsonAsync<T>(T data, ArtifactResourceKey key, OutputStreamOptions? options = null, CancellationToken cancellationToken = default)
        {
            EnsureNotDisposed();
            return _artifactDataManager.OutputJsonAsync(data, key, options, cancellationToken);
        }

        public ValueTask OutputJsonAsync<T>(T data, JsonTypeInfo<T> jsonTypeInfo, ArtifactResourceKey key, OutputStreamOptions? options = null, CancellationToken cancellationToken = default)
        {
            EnsureNotDisposed();
            return _artifactDataManager.OutputJsonAsync(data, jsonTypeInfo, key, options, cancellationToken);
        }

        public ValueTask OutputJsonAsync<T>(T data, JsonSerializerOptions jsonSerializerOptions, ArtifactResourceKey key, OutputStreamOptions? options = null, CancellationToken cancellationToken = default)
        {
            EnsureNotDisposed();
            return _artifactDataManager.OutputJsonAsync(data, jsonSerializerOptions, key, options, cancellationToken);
        }

        public ValueTask<Checksum> ComputeChecksumAsync(ArtifactResourceKey key, string checksumId, CancellationToken cancellationToken = default)
        {
            EnsureNotDisposed();
            return _artifactDataManager.ComputeChecksumAsync(key, checksumId, cancellationToken);
        }

        public ValueTask<Checksum?> GetChecksumAsync(ArtifactResourceKey key, CancellationToken cancellationToken = default)
        {
            EnsureNotDisposed();
            return _artifactDataManager.GetChecksumAsync(key, cancellationToken);
        }

        private void EnsureNotDisposed()
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }
            _disposed = true;
        }
    }
}
