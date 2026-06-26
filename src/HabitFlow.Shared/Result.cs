namespace HabitFlow.Shared;
public sealed record Error(string Code,string Message);
public class Result{public bool Succeeded{get;} public Error? Error{get;} protected Result(bool ok,Error? error){Succeeded=ok;Error=error;} public static Result Success()=>new(true,null); public static Result Failure(string code,string message)=>new(false,new Error(code,message));}
public sealed class Result<T>:Result{public T? Value{get;} private Result(bool ok,T? value,Error? error):base(ok,error){Value=value;} public static Result<T> Success(T value)=>new(true,value,null); public new static Result<T> Failure(string code,string message)=>new(false,default,new Error(code,message));}
public static class AppConstants{public const int FreePlanHabitLimit=5; public const string PrimaryColor="#10B981";}
