namespace Devbraco.Journeys;

public abstract class Rule
{
    public static Rule Required() => new RequiredRule();

    public static Rule Email() => new EmailRule();

    public static Rule MaxLength(int length) => new MaxLengthRule(length);

    public static Rule MinAge(int years) => new MinAgeRule(years);

    public abstract string Code { get; }

    public abstract string DefaultMessage { get; }

    public abstract bool TryValidate(string? value, out string message);

    private sealed class RequiredRule : Rule
    {
        public override string Code => "required";

        public override string DefaultMessage => "This field is required.";

        public override bool TryValidate(string? value, out string message)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                message = "";
                return true;
            }

            message = DefaultMessage;
            return false;
        }
    }

    private sealed class EmailRule : Rule
    {
        public override string Code => "email";

        public override string DefaultMessage => "Enter a valid email address.";

        public override bool TryValidate(string? value, out string message)
        {
            if (string.IsNullOrWhiteSpace(value)
                || System.Net.Mail.MailAddress.TryCreate(value, out var address) && address.Address == value.Trim())
            {
                message = "";
                return true;
            }

            message = DefaultMessage;
            return false;
        }
    }

    private sealed class MaxLengthRule(int length) : Rule
    {
        public override string Code => "maxLength";

        public override string DefaultMessage => $"Use {length} characters or fewer.";

        public override bool TryValidate(string? value, out string message)
        {
            if (string.IsNullOrEmpty(value) || value.Length <= length)
            {
                message = "";
                return true;
            }

            message = DefaultMessage;
            return false;
        }
    }

    private sealed class MinAgeRule(int years) : Rule
    {
        public override string Code => "minAge";

        public override string DefaultMessage => $"You need to be {years} or older.";

        public override bool TryValidate(string? value, out string message)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                message = "";
                return true;
            }

            if (!DateOnly.TryParseExact(value, "yyyy-MM-dd", out var date))
            {
                message = "Enter a valid date.";
                return false;
            }

            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var age = today.Year - date.Year;
            if (date > today.AddYears(-age))
            {
                age--;
            }

            if (age >= years)
            {
                message = "";
                return true;
            }

            message = DefaultMessage;
            return false;
        }
    }
}
