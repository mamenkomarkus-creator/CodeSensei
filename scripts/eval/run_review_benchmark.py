#!/usr/bin/env python3
"""Прогін набору docs/evaluation/benchmark.json через живий CodeSensei API.

Для кожного фрагмента: POST /api/code/submit, далі опитування /api/inbox
(як термінал, але раз на секунду), заміряє час до готового рев'ю.
Результат пишеться у JSON; оцінка влучання робиться вручну за полем "defect".

Використання: python3 scripts/eval/run_review_benchmark.py --base URL --out FILE [--repeats 2]
"""
import argparse, json, random, time, urllib.request, urllib.error

ALPHABET = "23456789ABCDEFGHJKLMNPQRSTUVWXYZ"


def call(method, url, body=None):
    data = json.dumps(body).encode() if body is not None else None
    req = urllib.request.Request(url, data=data, method=method,
                                 headers={"Content-Type": "application/json"})
    try:
        with urllib.request.urlopen(req, timeout=60) as r:
            return r.status, json.loads(r.read().decode())
    except urllib.error.HTTPError as e:
        return e.code, json.loads(e.read().decode() or "{}")


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--base", required=True)
    ap.add_argument("--token", default="secret123")
    ap.add_argument("--bench", default="docs/evaluation/benchmark.json")
    ap.add_argument("--out", required=True)
    ap.add_argument("--repeats", type=int, default=2)
    ap.add_argument("--timeout", type=float, default=120)
    a = ap.parse_args()

    bench = json.load(open(a.bench, encoding="utf-8"))
    runs = []
    for rep in range(1, a.repeats + 1):
        for item in bench:
            code = "".join(random.choice(ALPHABET) for _ in range(5))
            t0 = time.perf_counter()
            st, resp = call("POST", f"{a.base}/api/code/submit",
                            {"code": item["code"], "language": "csharp", "ticketCode": code})
            t_submit = time.perf_counter() - t0
            rec = {"id": item["id"], "rep": rep, "ticket": code, "submit_http": st,
                   "submit_s": round(t_submit, 3), "status": None, "latency_s": None, "lines": None}
            if st == 200:
                while time.perf_counter() - t0 < a.timeout:
                    time.sleep(1.0)
                    _, inbox = call("GET", f"{a.base}/api/inbox?room=metalab&k={a.token}")
                    hit = next((i for i in inbox.get("items", []) if i["code"] == code), None)
                    if hit:
                        rec["latency_s"] = round(time.perf_counter() - t0, 2)
                        rec["status"] = hit["status"]
                        rec["lines"] = hit["lines"]
                        break
                else:
                    rec["status"] = "timeout"
            print(item["id"], rep, rec["status"], rec["latency_s"], flush=True)
            runs.append(rec)
            time.sleep(1.5)
    meta = {"base": a.base, "started_utc": time.strftime("%Y-%m-%dT%H:%M:%SZ", time.gmtime()),
            "repeats": a.repeats}
    json.dump({"meta": meta, "runs": runs}, open(a.out, "w", encoding="utf-8"),
              ensure_ascii=False, indent=2)


if __name__ == "__main__":
    main()
