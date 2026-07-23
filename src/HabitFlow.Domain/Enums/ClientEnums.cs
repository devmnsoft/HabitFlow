namespace HabitFlow.Domain;

public enum ClientStatus { Active, Inactive, Blocked }
public enum ClientPlan { Free, Premium, Enterprise }
public enum ClientPersonType { NaturalPerson, LegalPerson }
public enum ClientDocumentType { CPF, CNPJ }
public enum ClientSubscriptionStatus { Free, Trial, Active, PastDue, Canceled, Suspended }
public enum ClientBenefitsStatus { Free, PremiumActive, PremiumBlocked, EnterpriseActive, EnterpriseBlocked }
public enum ClientPaymentStatus { None, Pending, Approved, Rejected, Canceled, Expired, Overdue, Refunded }
