from __future__ import annotations

import shutil
from pathlib import Path

ROOT = Path(__file__).resolve().parent

EXTENSIONS = {
    ".cs",
    ".cshtml",
    ".json",
    ".md",
    ".js",
    ".ts",
    ".css",
    ".html",
}

EXCLUDED_DIRS = {
    ".git",
    ".vs",
    ".vscode",
    "bin",
    "obj",
    "node_modules",
    "wwwroot/lib",
}

# Các dấu hiệu phổ biến khi UTF-8 bị đọc nhầm thành Windows-1252.
MOJIBAKE_MARKERS = (
    "Ã",
    "Â",
    "Ä",
    "Æ",
    "áº",
    "á»",
    "â‚",
    "â€“",
    "â€”",
    "â€œ",
    "â€",
    "ðŸ",
)

# Ký tự thay thế này cho thấy dữ liệu đã mất.
REPLACEMENT_CHAR = "\ufffd"


def is_excluded(path: Path) -> bool:
    relative_parts = path.relative_to(ROOT).parts

    for index, part in enumerate(relative_parts):
        if part in {".git", ".vs", ".vscode", "bin", "obj", "node_modules"}:
            return True

        if (
            index + 1 < len(relative_parts)
            and part == "wwwroot"
            and relative_parts[index + 1] == "lib"
        ):
            return True

    return False


def mojibake_score(text: str) -> int:
    score = sum(text.count(marker) for marker in MOJIBAKE_MARKERS)

    # Ký tự replacement là dấu hiệu nghiêm trọng hơn.
    score += text.count(REPLACEMENT_CHAR) * 10

    return score


def decode_one_layer(text: str) -> str | None:
    """
    Đảo trường hợp:
        UTF-8 bytes -> bị đọc nhầm thành Windows-1252.

    Ví dụ:
        'SÃ¢n' -> 'Sân'
    """
    try:
        return text.encode("cp1252").decode("utf-8")
    except (UnicodeEncodeError, UnicodeDecodeError):
        return None


def repair_text(text: str) -> tuple[str, int]:
    """
    Sửa từng token có dấu hiệu mojibake thay vì chuyển toàn bộ file.
    Nhờ đó chữ Việt đang đúng trong cùng file sẽ được giữ nguyên.
    """
    import re

    repaired_count = 0

    def repair_token(match: re.Match[str]) -> str:
        nonlocal repaired_count

        token = match.group(0)

        if mojibake_score(token) == 0:
            return token

        current = token
        current_score = mojibake_score(current)

        for _ in range(3):
            candidate = decode_one_layer(current)
            if candidate is None:
                break

            candidate_score = mojibake_score(candidate)

            if candidate_score >= current_score:
                break

            current = candidate
            current_score = candidate_score

        if current != token:
            repaired_count += 1

        return current

    repaired = re.sub(r"\S+", repair_token, text)
    return repaired, repaired_count

def main() -> None:
    scanned = 0
    changed = 0
    suspicious_but_unfixed: list[Path] = []

    for path in ROOT.rglob("*"):
        if not path.is_file():
            continue

        if path.suffix.lower() not in EXTENSIONS:
            continue

        if is_excluded(path):
            continue

        scanned += 1

        try:
            original = path.read_text(encoding="utf-8-sig")
        except UnicodeDecodeError:
            print(f"[SKIP: không phải UTF-8] {path.relative_to(ROOT)}")
            continue

        original_score = mojibake_score(original)
        if original_score == 0:
            continue

        repaired, layers = repair_text(original)
        repaired_score = mojibake_score(repaired)

        if layers == 0 or repaired_score >= original_score:
            suspicious_but_unfixed.append(path)
            print(
                f"[CẦN KIỂM TRA] {path.relative_to(ROOT)} "
                f"(điểm lỗi: {original_score})"
            )
            continue

        backup = path.with_name(path.name + ".encoding-backup")
        if not backup.exists():
            shutil.copy2(path, backup)

        # Ghi UTF-8 không BOM.
        path.write_text(repaired, encoding="utf-8", newline="")
        changed += 1

        print(
            f"[ĐÃ SỬA] {path.relative_to(ROOT)} "
            f"({layers} lớp, {original_score} -> {repaired_score})"
        )

    print()
    print(f"Đã quét: {scanned} file")
    print(f"Đã sửa: {changed} file")
    print(f"Cần kiểm tra thủ công: {len(suspicious_but_unfixed)} file")

    if suspicious_but_unfixed:
        print("\nCác file chưa thể tự sửa:")
        for path in suspicious_but_unfixed:
            print(f"  - {path.relative_to(ROOT)}")


if __name__ == "__main__":
    main()