using System.CommandLine;
using Art.Common.Management;

namespace Art.Tesler.Tests;

internal class SharedMemoryRegistrationProvider : ITeslerRegistrationProvider
{
    private readonly InMemoryArtifactRegistrationManager _artifactRegistrationManager = new();

    public void Initialize(Command command)
    {
    }

    public Type GetArtifactRegistrationManagerType() => typeof(PersistentProxyArtifactRegistrationManager);

    public IArtifactRegistrationManager CreateArtifactRegistrationManager(ParseResult parseResult)
    {
        return new PersistentProxyArtifactRegistrationManager(_artifactRegistrationManager);
    }

    private class PersistentProxyArtifactRegistrationManager : IArtifactRegistrationManager
    {
        private readonly IArtifactRegistrationManager _artifactRegistrationManager;
        private bool _disposed;

        public PersistentProxyArtifactRegistrationManager(IArtifactRegistrationManager artifactRegistrationManager)
        {
            _artifactRegistrationManager = artifactRegistrationManager;
        }

        public Task<List<ArtifactInfo>> ListArtifactsAsync(CancellationToken cancellationToken = default)
        {
            EnsureNotDisposed();
            return _artifactRegistrationManager.ListArtifactsAsync(cancellationToken);
        }

        public Task<List<ArtifactInfo>> ListArtifactsAsync(Func<ArtifactInfo, bool> predicate, CancellationToken cancellationToken = default)
        {
            EnsureNotDisposed();
            return _artifactRegistrationManager.ListArtifactsAsync(predicate, cancellationToken);
        }

        public Task<List<ArtifactInfo>> ListArtifactsAsync(string tool, CancellationToken cancellationToken = default)
        {
            EnsureNotDisposed();
            return _artifactRegistrationManager.ListArtifactsAsync(tool, cancellationToken);
        }

        public Task<List<ArtifactInfo>> ListArtifactsAsync(string tool, string group, CancellationToken cancellationToken = default)
        {
            EnsureNotDisposed();
            return _artifactRegistrationManager.ListArtifactsAsync(tool, group, cancellationToken);
        }

        public ValueTask AddArtifactAsync(ArtifactInfo artifactInfo, CancellationToken cancellationToken = default)
        {
            EnsureNotDisposed();
            return _artifactRegistrationManager.AddArtifactAsync(artifactInfo, cancellationToken);
        }

        public Task<List<ArtifactResourceInfo>> ListResourcesAsync(ArtifactKey key, CancellationToken cancellationToken = default)
        {
            EnsureNotDisposed();
            return _artifactRegistrationManager.ListResourcesAsync(key, cancellationToken);
        }

        public ValueTask AddResourceAsync(ArtifactResourceInfo artifactResourceInfo, CancellationToken cancellationToken = default)
        {
            EnsureNotDisposed();
            return _artifactRegistrationManager.AddResourceAsync(artifactResourceInfo, cancellationToken);
        }

        public ValueTask<ArtifactInfo?> TryGetArtifactAsync(ArtifactKey key, CancellationToken cancellationToken = default)
        {
            EnsureNotDisposed();
            return _artifactRegistrationManager.TryGetArtifactAsync(key, cancellationToken);
        }

        public ValueTask<ArtifactResourceInfo?> TryGetResourceAsync(ArtifactResourceKey key, CancellationToken cancellationToken = default)
        {
            EnsureNotDisposed();
            return _artifactRegistrationManager.TryGetResourceAsync(key, cancellationToken);
        }

        public ValueTask RemoveArtifactAsync(ArtifactKey key, CancellationToken cancellationToken = default)
        {
            EnsureNotDisposed();
            return _artifactRegistrationManager.RemoveArtifactAsync(key, cancellationToken);
        }

        public ValueTask RemoveResourceAsync(ArtifactResourceKey key, CancellationToken cancellationToken = default)
        {
            EnsureNotDisposed();
            return _artifactRegistrationManager.RemoveResourceAsync(key, cancellationToken);
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
