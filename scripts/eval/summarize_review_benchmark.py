#!/usr/bin/env python3
"""Зведення результатів run_review_benchmark.py: затримка (p50/p95) і відповідність формату.

Влучання (чи знайдено закладену помилку) оцінюється вручну і тут не обчислюється.
Використання: python3 scripts/eval/summarize_review_benchmark.py results.json
"""
import json, re, sys

MD = re.compile(r"[*#`]|^\s*[-•]\s", re.M)


def pct(values, p):
    v = sorted(values)
    k = (len(v) - 1) * p
    f, c = int(k), min(int(k) + 1, len(v) - 1)
    return v[f] + (v[c] - v[f]) * (k - f)


def main(path):
    runs = json.load(open(path, encoding="utf-8"))["runs"]
    done = [r for r in runs if r["status"] == "completed"]
    print(f"запусків: {len(runs)}, completed: {len(done)}, error: "
          f"{sum(r['status'] == 'error' for r in runs)}, timeout: "
          f"{sum(r['status'] == 'timeout' for r in runs)}")
    if not done:
        print("Немає завершених рев'ю: перевірте Gemini__ApiKey на сервері.")
        return
    lat = [r["latency_s"] for r in done]
    print(f"затримка до готового рев'ю, с: p50={pct(lat, .5):.1f} p95={pct(lat, .95):.1f} "
          f"min={min(lat):.1f} max={max(lat):.1f} (опитування раз на 1 с)")
    ok_len = sum(max(len(l) for l in r["lines"]) <= 55 for r in done)
    ok_cnt = sum(len(r["lines"]) <= 8 for r in done)
    ok_md = sum(not MD.search("\n".join(r["lines"])) for r in done)
    ok_sec = sum(all(any(l.startswith(h) for l in r["lines"]) for h in ("Помилки:", "ООП:", "Підказка:"))
                 for r in done)
    n = len(done)
    print(f"рядки ≤55 символів: {ok_len}/{n}; ≤8 рядків: {ok_cnt}/{n}; "
          f"без markdown: {ok_md}/{n}; є три секції: {ok_sec}/{n}")


if __name__ == "__main__":
    main(sys.argv[1])
