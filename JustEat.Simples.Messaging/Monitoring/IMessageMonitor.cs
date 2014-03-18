namespace JustEat.Simples.NotificationStack.Messaging.Monitoring
{
    public interface IMessageMonitor
    {
        void HandleException(string messageType);
        void HandleTime(long handleTimeMs);
        void IssuePublishingMessage();
        void IncrementThrottlingStatistic();
        void HandleThrottlingTime(long handleTimeMs);
    }
}