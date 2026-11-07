using System.Diagnostics;

// 확인용 로그. 평소에는 아무것도 출력하지 않는다 (호출 자체가 빠짐 → 렉 없음)
// 로그를 보고 싶을 때: Edit > Project Settings > Player > Other Settings >
//   Scripting Define Symbols에 WHISKER_LOG 추가
public static class GameLog
{
    [Conditional("WHISKER_LOG")]
    public static void Info(string message)
    {
        UnityEngine.Debug.Log(message);
    }
}
