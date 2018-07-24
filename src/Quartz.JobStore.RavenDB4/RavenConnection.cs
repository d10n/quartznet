using System;
using System.Threading;
using System.Threading.Tasks;

using Raven.Client.Documents.Linq;
using Raven.Client.Documents.Session;

namespace Quartz.Impl.RavenDB
{
    public class RavenConnection : IDisposable
    {
        private IAsyncDocumentSession session;
        private readonly string schedulerName;
        private DateTimeOffset? sigChangeForTxCompletion;

        public RavenConnection(IAsyncDocumentSession session, string schedulerName)
        {
            this.session = session;
            this.schedulerName = schedulerName;
        }

        internal virtual DateTimeOffset? SignalSchedulingChangeOnTxCompletion
        {
            get => sigChangeForTxCompletion;
            set
            {
                DateTimeOffset? sigTime = sigChangeForTxCompletion;
                if (sigChangeForTxCompletion == null && value.HasValue)
                {
                    sigChangeForTxCompletion = value;
                }
                else
                {
                    if (sigChangeForTxCompletion == null || value < sigTime)
                    {
                        sigChangeForTxCompletion = value;
                    }
                }
            }
        }

        internal Task Commit(CancellationToken cancellationToken)
        {
            return session.SaveChangesAsync(cancellationToken);
        }

        internal void Rollback()
        {
            session?.Dispose();
            session = null;
        }

        internal IRavenQueryable<Trigger> QueryTriggers()
        {
            return session.Query<Trigger, TriggerIndex>()
                .Where(x => x.Scheduler == schedulerName);
        }

        internal IRavenQueryable<Job> QueryJobs()
        {
            return session.Query<Job, JobIndex>()
                .Where(x => x.Scheduler == schedulerName);
        }

        internal IRavenQueryable<FiredTrigger> QueryFiredTriggers()
        {
            return session.Query<FiredTrigger, FiredTriggerIndex>()
                .Where(x => x.Scheduler == schedulerName);
        }

        internal Task<Scheduler> LoadScheduler(CancellationToken cancellationToken)
        {
            return session.LoadAsync<Scheduler>(schedulerName, cancellationToken);
        }

        internal Task<Trigger> LoadTrigger(TriggerKey triggerKey, CancellationToken cancellationToken)
        {
            return session.LoadAsync<Trigger>(triggerKey.DocumentId(schedulerName), cancellationToken);
        }

        internal Task<Job> LoadJob(JobKey jobKey, CancellationToken cancellationToken)
        {
            return session.LoadAsync<Job>(jobKey.DocumentId(schedulerName), cancellationToken);
        }

        internal Task<Job> LoadJob(string id, CancellationToken cancellationToken)
        {
            return session.LoadAsync<Job>(id, cancellationToken);
        }

        internal Task<bool> ExistsAsync(string id)
        {
            return session.Advanced.ExistsAsync(id);
        }

        internal Task StoreAsync(object entity, string id, CancellationToken cancellationToken)
        {
            return session.StoreAsync(entity, id, cancellationToken);
        }

        internal void Delete(object entity)
        {
            session.Delete(entity);
        }

        internal void Delete(string id)
        {
            session.Delete(id);
        }

        public void Dispose()
        {
            session?.Dispose();
            session = null;
        }
    }
}