using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

// 언어 (영어 / 한국어). 오브젝트에 붙이지 않음
// 게임 속 문구는 모두 한국어로 적혀 있고, 영어일 때 아래 사전(En)에서 찾아 바꿔 보여준다
// 사전에 통째로 없으면 줄(\n)과 두 칸 이상 띄어쓰기로 나눠 조각마다 찾는다
//   예) "↑  두꺼비집 작동하기" → "↑" + "  " + "Operate fuse box"
// 이름이 끼어드는 문구는 F로: Loc.F("{0} 획득", 이름) → "Got {0}"
// 기본 언어는 영어. Esc 설정에서 바꾸면 PlayerPrefs에 저장된다
public static class Loc
{
    private const string Key = "opt_lang";

    public static bool Korean => PlayerPrefs.GetInt(Key, 0) == 1;
    public static event System.Action Changed;

    public static void SetKorean(bool on)
    {
        PlayerPrefs.SetInt(Key, on ? 1 : 0);
        PlayerPrefs.Save();
        Changed?.Invoke();
    }

    // 한국어 원문 → 지금 언어
    public static string T(string ko)
    {
        if (string.IsNullOrEmpty(ko) || Korean) return ko;
        if (En.TryGetValue(ko, out var en)) return en;

        // 조각으로 나눠서 찾기 (구분자는 그대로 둠)
        var parts = Regex.Split(ko, @"(\n|\s{2,})");
        if (parts.Length <= 1) return ko;
        var sb = new StringBuilder();
        foreach (var p in parts) sb.Append(En.TryGetValue(p, out var e) ? e : p);
        return sb.ToString();
    }

    // 이름 등이 끼어드는 문구. 인자는 부르는 쪽에서 이미 T로 바꿔서 넘김
    public static string F(string koFormat, params object[] args)
    {
        string fmt = Korean ? koFormat : (En.TryGetValue(koFormat, out var e) ? e : koFormat);
        return string.Format(fmt, args);
    }

    private static readonly Dictionary<string, string> En = new Dictionary<string, string>
    {
        // ---------- 타이틀·설정 ----------
        { "고양이만 볼 수 있는 것", "What Only the Cat Can See" },
        { "폐병원에 갇힌 주인은 그곳을 떠도는 '존재'를 보지 못한다.", "Trapped in an abandoned hospital, the owner cannot see the 'Presence' that roams it." },
        { "그것을 볼 수 있는 건 고양이뿐.", "Only the cat can see it." },
        { "게임 시작", "Play" },
        { "시작하기", "Start" },
        { "설정", "Settings" },
        { "일시정지", "Paused" },
        { "계속하기", "Resume" },
        { "소리 크기", "Volume" },
        { "프레임 제한", "Frame Limit" },
        { "FPS 표시", "Show FPS" },
        { "전체 화면", "Fullscreen" },
        { "언어", "Language" },
        { "처음부터 다시", "Restart" },
        { "타이틀로", "Back to Title" },
        { "뒤로", "Back" },
        { "모니터 맞춤", "V-Sync" },
        { "켜기", "On" },
        { "끄기", "Off" },
        { "빌드에서만", "Build only" },
        { "↑ ↓ 고르기", "↑ ↓ Select" },
        { "← → 바꾸기", "← → Change" },
        { "Enter 선택", "Enter Confirm" },
        { "Esc 닫기", "Esc Close" },
        { "닫기", "Close" },

        // ---------- 게임오버·탈출 ----------
        { "잡혔다…", "Caught…" },
        { "처음부터 다시 시작", "Restart from the beginning" },
        { "탈출 성공", "Escaped!" },
        { "주인과 함께 폐병원을 빠져나왔다", "You escaped the hospital with your owner" },
        { "다시 하기", "Play again" },
        { "플레이 시간  {0}", "Play time  {0}" },

        // ---------- 인벤토리·아이템 ----------
        { "인벤토리", "Inventory" },
        { "인벤토리 · 설정", "Inventory · Settings" },
        { "Tab / Esc 닫기", "Tab / Esc Close" },
        { "Tab 닫기", "Tab Close" },
        { "가진 아이템이 없다", "No items" },
        { "{0} 획득", "Got {0}" },
        { "(필요: {0})", "(Needs: {0})" },
        { "({0} 필요)", "(Needs {0})" },
        { "녹슨 열쇠", "Rusty Key" },
        { "어딘가의 문을 열 수 있을 것 같다.", "It might open a door somewhere." },
        { "퓨즈", "Fuse" },
        { "두꺼비집에 끼우면 전기가 들어올 것 같다.", "Putting it in the fuse box might bring the power back." },
        { "열쇠 조각 (손잡이)", "Key Piece (Handle)" },
        { "부러진 열쇠의 손잡이 쪽이다. 나머지 조각이 어딘가에 있을 것 같다.", "The handle half of a broken key. The other half must be somewhere." },
        { "열쇠 조각 (끝)", "Key Piece (Tip)" },
        { "부러진 열쇠의 끝부분이다. 손잡이 쪽 조각과 맞을 것 같다.", "The tip of a broken key. It should fit the handle half." },
        { "땜납", "Solder" },
        { "금속을 붙일 때 쓰는 납 실. 녹일 도구가 있으면 쓸 수 있을 것 같다.", "Solder wire for joining metal. Needs something hot to melt it." },
        { "출구 열쇠", "Exit Key" },
        { "조각을 붙여 만든 열쇠. 로비 정문을 열 수 있을 것 같다.", "A key rebuilt from its pieces. It should open the lobby's front door." },

        // ---------- 팝업 ----------
        { "금고", "Safe" },
        { "← → 자리 이동", "← → Select digit" },
        { "↑ ↓ 숫자 바꾸기", "↑ ↓ Change digit" },
        { "숫자 키 입력", "0-9 Type" },
        { "Enter 확인", "Enter Confirm" },
        { "딸깍… 열리지 않는다", "Click… It won't open" },
        { "간호 일지", "Nurse's Log" },
        { "쪽지 읽기", "Read note" },
        { "시설과 메모", "Facilities Memo" },
        { "예비 퓨즈 보관 위치", "Spare fuse locations" },
        { "- 지하 보일러실 배전반 옆", "- Next to the panel in the basement boiler room" },
        { "- 2층 간호사실 사물함", "- Locker in the 2F nurses' station" },
        { "- 1층 대기실 벽시계 뒤", "- Behind the wall clock in the 1F waiting room" },
        { "- 수술실 약품 선반 아래", "- Under the medicine shelf in the operating room" },
        { "…오늘부터 새벽 회진 순서를 바꾼다.", "…Starting today, the dawn rounds go in a new order." },
        { "해가 뜨는 색의 침대에서 시작해,", "Begin at the bed the color of sunshine," },
        { "숲을 지나,", "pass through the forest," },
        { "피를 본 뒤,", "then see the blood," },
        { "바다에서 끝낸다.", "and end at the sea." },
        { "순서를 잊지 말 것. 진료실 금고 번호도 같은 순서다.", "Don't forget the order. The exam room safe uses the same order." },

        // ---------- 조합 게이지 ----------
        { "그만두기", "Cancel" },
        { "작업을 멈췄다", "Cancelled" },
        { "작업하는 중…", "Working…" },
        { "녹슨 두꺼비집에 퓨즈를 끼우는 중…", "Fitting the fuse into the rusty fuse box…" },
        { "전기 소작기로 열쇠 조각을 붙이는 중…", "Welding the key pieces with a cautery…" },

        // ---------- 안내 (↑ / ↓) ----------
        { "조사하기", "Inspect" },
        { "시계 조사하기", "Inspect clock" },
        { "두꺼비집 작동하기", "Operate fuse box" },
        { "두꺼비집 조사하기", "Inspect fuse box" },
        { "퓨즈 끼우기", "Insert fuse" },
        { "소화기함 살펴보기", "Check extinguisher cabinet" },
        { "화분 살펴보기", "Check flowerpot" },
        { "문서 읽기", "Read document" },
        { "금고 조사하기", "Inspect safe" },
        { "수술대 조사하기", "Inspect operating table" },
        { "열쇠 조각 붙이기", "Join key pieces" },
        { "침대 살펴보기", "Check bed" },
        { "벽 타기", "Climb wall" },
        { "내려가기", "Climb down" },
        { "나오기", "Come out" },
        { "↓  주인과 {0}에 숨기", "↓  Hide behind the {0} with your owner" },
        { "↓  {0}에 숨기", "↓  Hide behind the {0}" },
        { "박스", "box" },
        { "상자 더미", "box pile" },
        { "책상", "desk" },
        { "수술대", "operating table" },
        { "문 열기", "Open door" },
        { "탈출하기", "Escape" },
        { "주인과 {0}", "{0} with your owner" },
        { "(주인은 못 따라옴)", "(your owner can't follow)" },
        { "계단 이용하기", "Use stairs" },
        { "지하로 내려가기", "Go down to basement" },
        { "1층으로 올라가기", "Go up to 1F" },
        { "2층으로 올라가기", "Go up to 2F" },
        { "1층으로 내려가기", "Go down to 1F" },
        { "주인 공포도", "Owner's Fear" },
        { "주인이 따라오는 중", "Owner is following" },
        { "주인이 기다리는 중", "Owner is waiting" },
        { "주인이 숨어 있음", "Owner is hiding" },
        { "(몸이 굳는다…)", "(Frozen stiff…)" },

        // ---------- 메시지 ----------
        { "너무 높다. 밟고 올라설 것이 필요하다", "Too high. Need something to stand on." },
        { "전자 잠금으로 잠겨 있다. 전기가 들어오지 않는다", "Electronically locked. There's no power." },
        { "계단에서 무언가 내려오고 있다… 숨어야 한다!", "Something is coming down the stairs… Hide!" },
        { "잠겨 있다", "Locked" },
        { "문이 열렸다", "The door opened" },
        { "주인은 따라오지 못했다", "The owner couldn't follow" },
        { "쇠사슬로 잠겨 있다", "Chained shut" },
        { "주인과 함께 나가야 한다", "You must leave with your owner" },
        { "주인을 데리고 가야 한다", "You need to bring your owner" },
        { "주인이 비명을 질렀다! 무언가 소리를 듣고 온다…", "The owner screamed! Something heard it…" },
        { "쿵—! 전기가 들어왔다", "THUD—! The power is back" },
        { "2층으로 가는 문의 잠금이 풀렸다", "The door to 2F is unlocked" },
        { "소리가 너무 컸다… 무언가 내려온다. 주인과 함께 숨어야 한다!", "That was too loud… Something is coming. Hide with your owner!" },
        { "…지나갔다", "…It's gone" },
        { "들켰다…!", "Spotted…!" },
        { "위에서 무언가 떨어졌다…!", "Something dropped from above…!" },
        { "도망쳐! 주인과 함께 로비 정문으로!", "Run! To the lobby's front door with your owner!" },
        { "멈춰 버린 시계다. 바늘이 움직이지 않는다", "A stopped clock. The hands don't move." },
        { "시계 뒤에 무언가 붙어 있다… 퓨즈를 얻었다", "Something is stuck behind the clock… Got a fuse" },
        { "멈춘 시계다", "A stopped clock" },
        { "작동하지 않는다. 퓨즈가 하나 빠져 있다…", "It doesn't work. A fuse is missing…" },
        { "두꺼비집이다. 퓨즈가 하나 빠져 있다…", "A fuse box. A fuse is missing…" },
        { "퓨즈를 끼웠다", "Fuse inserted" },
        { "전기가 들어와 있다", "The power is on" },
        { "소화기 뒤에 무언가 끼워져 있다… 땜납을 얻었다", "Something is tucked behind the extinguisher… Got solder" },
        { "녹슨 소화기다", "A rusty extinguisher" },
        { "화분 흙 속에서 열쇠 조각을 찾았다", "Found a key piece in the soil" },
        { "깨진 화분이다", "A broken flowerpot" },
        { "철컥— 금고가 열렸다. 열쇠 조각을 얻었다", "Clunk— The safe opened. Got a key piece" },
        { "빈 금고다", "An empty safe" },
        { "부러진 열쇠를 여기서 붙일 수 있을 것 같다", "The broken key could be fixed here" },
        { "출구 열쇠가 완성됐다! 톡— 바닥에 떨어졌다", "The exit key is complete! Tink— it dropped to the floor" },
        { "피 묻은 수술대다", "A bloodstained operating table" },

        // ---------- 1차 테스트 뒤 추가 ----------
        { "굳게 잠겨 있다", "Firmly locked" },
        { "숨는 걸 들켰다…!", "It saw you hide…!" },
        { "숨는 걸 들켰다", "It saw you hide" },
        { "고양이가 잡혔다", "The cat was caught" },
        { "주인이 잡혔다", "Your owner was caught" },
        { "잡힌 횟수  {0}", "Times caught  {0}" },
        { "상자", "box" },
        { "안내데스크", "reception desk" },
        { "지하 창고 두꺼비집의 퓨즈가 또 나갔다.", "The fuse in the basement storage fuse box blew again." },
        { "예비 퓨즈는 1층 대기실 벽시계 뒤에 붙여 두었음.", "I taped a spare fuse behind the wall clock in the 1F waiting room." },
        { "(의자와 책상이 있는 방)", "(the room with the chair and the desk)" },
        { "시계가 높으니 의자를 밀어서 밟고 올라갈 것.", "The clock is high up. Push the chair under it and stand on it." },
        { "시계 뒤에 무언가 붙어 있다. 먼저 지하 두꺼비집을 확인해 보자", "Something is stuck behind the clock. Better check the basement fuse box first." },
        { "작동하지 않는다. 퓨즈가 하나 빠져 있다… 예비 퓨즈를 찾아야 한다", "It doesn't work. A fuse is missing… I need a spare one." },
        { "재료가 부족하다", "Not enough materials" },
    };
}
