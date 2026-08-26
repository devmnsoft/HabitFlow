namespace HabitFlow.Domain;

public enum SubscriptionStatus { Pending, PaymentPending, Active, Trial, Trialing, PastDue, Canceled, Expired, ManualReview, Failed, Inactive }
public enum BillingCycle { Monthly, Yearly }
public enum PaymentProvider { MercadoPago, Stripe, Manual, Dev }
public enum PaymentStatus { Pending, Approved, Rejected, Canceled, Refunded, Failed, Unknown }
