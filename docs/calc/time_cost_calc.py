"""Розрахунки для docs/time-cost-plan.md (лабораторна №5).

Усі вхідні дані взято з Jira-проєкту IME станом на 05.11.2026 (Velocity Chart,
Sprint Report, Time Tracking, Original Estimate Subtasks). Скрипт перевіряє формули
твердженнями (assert) і друкує результати, які перенесено у звіт.

Запуск: python3 docs/calc/time_cost_calc.py
"""

from datetime import date, timedelta
from fractions import Fraction as F
from math import ceil

# ---- Вхідні дані з Jira -------------------------------------------------------
EPIC_SP = {"IME-3": 13, "IME-4": 11, "IME-5": 13, "IME-6": 16}
MUST, SHOULD, COULD = 29, 11, 13            # MoSCoW-мітки Stories
SPRINT1 = {"committed": 16, "completed": 0, "time_spent_min": 0}
SPRINT2 = {"committed": 21, "completed": 21, "time_spent_min": 17}
SPRINT3 = {"committed": 19, "completed": 19, "time_spent_min": 36}
PLANNED_HOURS, PLANNED_SP = 106, 53         # Σ Original Estimate Subtasks (S2: 42h/21SP, S3: 38h/19SP, S4: 26h/13SP)
TEAM, HOURS_PER_SPRINT, RATE = 1, 40, 15    # люди, год/особу/спринт, $/год
CONTINGENCY, MANAGEMENT = F(10, 100), F(15, 100)
SPRINT2_START = date(2026, 11, 5)           # фактичний старт Sprint 2; спринт = 14 днів
SPRINT_DAYS = 14


def money(value) -> str:
    return f"${float(value):,.2f}".replace(",", " ")


def safe_div(numerator, denominator):
    """Повертає None, якщо знаменник 0 (показник математично не визначений)."""
    return None if denominator == 0 else F(numerator) / F(denominator)


# ---- 1. Вихідні дані ------------------------------------------------------------
backlog = sum(EPIC_SP.values())
assert backlog == MUST + SHOULD + COULD == 53
hours_per_sp = F(PLANNED_HOURS, PLANNED_SP)
assert hours_per_sp == 2
velocity_s2 = SPRINT2["completed"]
velocity_s3 = SPRINT3["completed"]
velocity = F(velocity_s2 + velocity_s3, 2)  # середня робоча Velocity (S2=21, S3=19) -> 20 SP
assert velocity == 20
velocity_avg_all = F(SPRINT1["completed"] + velocity_s2 + velocity_s3, 3)
assert round(float(velocity_avg_all), 1) == 13.3  # = «Средн.: 13.3» у Velocity Chart Jira

# ---- 3. Прогноз тривалості ------------------------------------------------------
sprints_total_work = ceil(F(backlog, velocity))
assert sprints_total_work == 3              # 53 / 20 = 2.65 → 3 робочі спринти (S2, S3, S4)
remaining_after_s2 = backlog - SPRINT1["completed"] - SPRINT2["completed"]
assert remaining_after_s2 == 32
remaining_after_s3 = remaining_after_s2 - SPRINT3["completed"]
assert remaining_after_s3 == 13             # залишок на Sprint 4 (v1.1: IME-17 + IME-18 = 13 SP)
sprints_left_after_s3 = ceil(F(remaining_after_s3, velocity))
assert sprints_left_after_s3 == 1           # Sprint 4 (13 SP <= 20 SP)
mvp_total = MUST + SHOULD
assert mvp_total == SPRINT2["completed"] + SPRINT3["completed"] == 40
sprint3_end = SPRINT2_START + timedelta(days=SPRINT_DAYS)        # планова дата релізу v1.0-MVP = 19.11.2026
sprint4_end = sprint3_end + timedelta(days=SPRINT_DAYS)          # планова дата завершення v1.1 = 03.12.2026
assert sprint3_end == date(2026, 11, 19) and sprint4_end == date(2026, 12, 3)
# Чутливість: середня Velocity по всіх 3 спринтах (13.33 SP)
sens_left_s3 = ceil(F(remaining_after_s3, 1) / velocity_avg_all)
assert sens_left_s3 == 1                    # 13 / 13.33 = 0.975 → 1 спринт (Sprint 4)

# ---- 4. Вартість команди ----------------------------------------------------------
team_cost = TEAM * HOURS_PER_SPRINT * RATE
assert team_cost == 600
cost_per_sp = F(team_cost, velocity)        # $600 / 20 SP = $30.00 (точно збігається з 2.0 год/SP × $15/год!)
assert cost_per_sp == 30
cost_per_sp_s2 = F(team_cost, velocity_s2)  # $600 / 21 SP = $28.57 (після Sprint 2)
assert round(float(cost_per_sp_s2), 2) == 28.57
assert safe_div(team_cost, SPRINT1["completed"]) is None    # за Velocity Sprint 1 — ділення на 0

# ---- 5. Вартість по Epics -------------------------------------------------------
epic_cost = {key: sp * cost_per_sp for key, sp in EPIC_SP.items()}
estimate = sum(epic_cost.values())
assert estimate == backlog * cost_per_sp == 1590

# ---- 6. Бюджет ----------------------------------------------------------------------
contingency = estimate * CONTINGENCY
working = estimate + contingency
management = working * MANAGEMENT
total = working + management
assert (float(estimate), float(contingency), float(working), float(management), round(float(total), 2)) == (
    1590.0, 159.0, 1749.0, 262.35, 2011.35
)

# ---- 7. Earned Value ------------------------------------------------------------
def evm(committed_sp, completed_sp, spent_min, sp_cost=cost_per_sp, tot_budget=total):
    pv = committed_sp * sp_cost
    ev = completed_sp * sp_cost
    ac = F(spent_min, 60) * RATE * TEAM
    cpi = safe_div(ev, ac)
    spi = safe_div(ev, pv)
    eac = None if not cpi else tot_budget / cpi
    return {"PV": pv, "EV": ev, "AC": ac, "CV": ev - ac, "SV": ev - pv, "CPI": cpi, "SPI": spi, "EAC": eac}


s1 = evm(SPRINT1["committed"], SPRINT1["completed"], SPRINT1["time_spent_min"])
s2 = evm(SPRINT2["committed"], SPRINT2["completed"], SPRINT2["time_spent_min"])
s3 = evm(SPRINT3["committed"], SPRINT3["completed"], SPRINT3["time_spent_min"])
cum_s1_s2 = evm(
    SPRINT1["committed"] + SPRINT2["committed"],
    SPRINT1["completed"] + SPRINT2["completed"],
    SPRINT1["time_spent_min"] + SPRINT2["time_spent_min"],
)
cum_all = evm(
    SPRINT1["committed"] + SPRINT2["committed"] + SPRINT3["committed"],
    SPRINT1["completed"] + SPRINT2["completed"] + SPRINT3["completed"],
    SPRINT1["time_spent_min"] + SPRINT2["time_spent_min"] + SPRINT3["time_spent_min"],
)
assert s1["CPI"] is None and s1["SPI"] == 0 and s1["EAC"] is None   # 0/0 — не визначено
assert s2["SPI"] == 1 and s2["AC"] == F(17, 4)                        # 17 хв × $15/год = $4.25
assert s3["SPI"] == 1 and s3["AC"] == F(36, 4) == 9                   # 36 хв × $15/год = $9.00
assert cum_all["EV"] == 1200 and cum_all["PV"] == 1680 and cum_all["AC"] == F(53, 4)  # $13.25
assert round(float(cum_all["SPI"]), 4) == round(40 / 56, 4)           # 40 / 56 = 0.7143

# Гіпотетичний сценарій (НЕ факт): робота виконується людиною за плановими Original Estimate (S2=42h, S3=38h -> 80h)
human_s2_ac, human_s3_ac = 42 * RATE, 38 * RATE
human_s2_cpi = s2["EV"] / human_s2_ac
human_s3_cpi = s3["EV"] / human_s3_ac
human_cum_ac = (42 + 38) * RATE
human_cum_cpi = cum_all["EV"] / human_cum_ac
assert human_s2_ac == 630 and human_s3_ac == 570 and human_cum_ac == 1200
assert round(float(human_s2_cpi), 2) == 1.00 and human_s3_cpi == 1 and human_cum_cpi == 1


def fmt(value):
    if value is None:
        return "не визначено"
    return f"{float(value):.4f}".rstrip("0").rstrip(".")


if __name__ == "__main__":
    print(f"Backlog {backlog} SP; h/SP {hours_per_sp}; Velocity S1={SPRINT1['completed']}, S2={velocity_s2}, S3={velocity_s3}, work_avg={float(velocity)}, all_avg={float(velocity_avg_all):.1f}")
    print(f"Sprints: total work {sprints_total_work}, left after S3 {sprints_left_after_s3} ({remaining_after_s3} SP); MVP {sprint3_end}, finish {sprint4_end}")
    print(f"Team cost {money(team_cost)}; cost/SP (avg 20) {money(cost_per_sp)} (S2-only was {money(cost_per_sp_s2)})")
    for key, value in epic_cost.items():
        print(f"  {key}: {EPIC_SP[key]} SP -> {money(value)}")
    print(f"  IME-6 split: v1.0 3 SP {money(3 * cost_per_sp)}, v1.1 13 SP {money(13 * cost_per_sp)}")
    print(f"Estimate {money(estimate)}; CR {money(contingency)}; Working {money(working)}; MR {money(management)}; Total {money(total)}")
    for name, data in (("Sprint 1", s1), ("Sprint 2", s2), ("Sprint 3", s3), ("S1+S2", cum_s1_s2), ("S1+S2+S3", cum_all)):
        print(name, {k: (money(v) if k in ("PV", "EV", "AC", "CV", "SV", "EAC") and v is not None else fmt(v)) for k, v in data.items()})
    print(f"Hypothetical human S2+S3 (80h): AC {money(human_cum_ac)}, CPI {fmt(human_cum_cpi)}")
    print("All assertions passed.")
