namespace HabitFlow.Domain;

public enum SubscriptionStatus { Pending, Active, Trial, PastDue, Canceled, Expired, Failed, Inactive }
public enum BillingCycle { Monthly, Yearly }
public enum PaymentProvider { MercadoPago, Stripe, Manual, Dev }
public enum PaymentStatus { Pending, Approved, Rejected, Canceled, Refunded, Failed, Unknown }
