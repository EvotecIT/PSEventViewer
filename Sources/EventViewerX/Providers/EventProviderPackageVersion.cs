namespace EventViewerX.Providers;

internal sealed class EventProviderPackageVersion :
    IComparable<EventProviderPackageVersion> {

    private EventProviderPackageVersion(
        int major,
        int minor,
        int patch,
        string prerelease) {

        Major = major;
        Minor = minor;
        Patch = patch;
        Prerelease = prerelease;
    }

    internal int Major { get; }
    internal int Minor { get; }
    internal int Patch { get; }
    internal string Prerelease { get; }

    internal static EventProviderPackageVersion Parse(string value) {
        if (string.IsNullOrWhiteSpace(value)) {
            throw new FormatException(
                "Provider package version cannot be empty.");
        }
        string[] buildParts = value.Split('+');
        if (buildParts.Length > 2 ||
            (buildParts.Length == 2 &&
             !ValidIdentifiers(
                 buildParts[1],
                 rejectNumericLeadingZero: false))) {
            throw new FormatException(
                $"Provider package version '{value}' contains invalid build metadata.");
        }
        string withoutBuild = buildParts[0];
        string[] releaseParts = withoutBuild.Split(
            new[] { '-' },
            2);
        string[] numbers = releaseParts[0].Split('.');
        if (numbers.Length != 3 ||
            !TryNumber(numbers[0], out int major) ||
            !TryNumber(numbers[1], out int minor) ||
            !TryNumber(numbers[2], out int patch)) {
            throw new FormatException(
                $"Provider package version '{value}' must be SemVer in major.minor.patch form.");
        }
        string prerelease =
            releaseParts.Length == 2
                ? releaseParts[1]
                : string.Empty;
        if (releaseParts.Length == 2 &&
            !ValidIdentifiers(
                prerelease,
                rejectNumericLeadingZero: true)) {
            throw new FormatException(
                $"Provider package version '{value}' contains an invalid prerelease identifier.");
        }
        return new EventProviderPackageVersion(
            major,
            minor,
            patch,
            prerelease);
    }

    public int CompareTo(EventProviderPackageVersion? other) {
        if (other == null) {
            return 1;
        }
        int comparison = Major.CompareTo(other.Major);
        if (comparison != 0) {
            return comparison;
        }
        comparison = Minor.CompareTo(other.Minor);
        if (comparison != 0) {
            return comparison;
        }
        comparison = Patch.CompareTo(other.Patch);
        if (comparison != 0) {
            return comparison;
        }
        if (Prerelease.Length == 0) {
            return other.Prerelease.Length == 0 ? 0 : 1;
        }
        if (other.Prerelease.Length == 0) {
            return -1;
        }

        string[] left = Prerelease.Split('.');
        string[] right = other.Prerelease.Split('.');
        int length = Math.Max(left.Length, right.Length);
        for (int index = 0; index < length; index++) {
            if (index >= left.Length) {
                return -1;
            }
            if (index >= right.Length) {
                return 1;
            }
            bool leftNumeric = int.TryParse(
                left[index],
                out int leftNumber);
            bool rightNumeric = int.TryParse(
                right[index],
                out int rightNumber);
            if (leftNumeric && rightNumeric) {
                comparison = leftNumber.CompareTo(rightNumber);
            } else if (leftNumeric) {
                comparison = -1;
            } else if (rightNumeric) {
                comparison = 1;
            } else {
                comparison = string.CompareOrdinal(
                    left[index],
                    right[index]);
            }
            if (comparison != 0) {
                return comparison;
            }
        }
        return 0;
    }

    private static bool TryNumber(string value, out int number) {
        if (value.Length > 1 && value[0] == '0') {
            number = 0;
            return false;
        }
        return int.TryParse(value, out number) && number >= 0;
    }

    private static bool ValidIdentifiers(
        string value,
        bool rejectNumericLeadingZero) {

        if (value.Length == 0) {
            return false;
        }
        foreach (string identifier in value.Split('.')) {
            if (identifier.Length == 0 ||
                identifier.Any(static character =>
                    !char.IsLetterOrDigit(character) &&
                    character != '-')) {
                return false;
            }
            if (rejectNumericLeadingZero &&
                identifier.Length > 1 &&
                identifier[0] == '0' &&
                identifier.All(char.IsDigit)) {
                return false;
            }
        }
        return true;
    }
}
