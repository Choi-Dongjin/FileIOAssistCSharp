using System.Text;

namespace FileIOAssist
{
    internal class StringControl
    {
        // 문자열이 정규화되었는지 확인하는 함수
        internal static bool IsNormalized(string input)
        {
            string normalized = input.Normalize(NormalizationForm.FormC);
            return input.Equals(normalized, StringComparison.OrdinalIgnoreCase);
        }

        // 문자열을 정규화하는 함수 (FormD 형식)
        internal static string NormalizeString(string input)
        {
            return input.Normalize(NormalizationForm.FormC);
        }
    }
}
