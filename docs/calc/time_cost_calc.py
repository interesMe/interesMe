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
PLANNED_HOURS, PLANNED_SP = 80, 40          # Σ Original Estimate Subtasks Sprint 2 + Sprint 3
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
velocity = SPRINT2["completed"]             # перша достовірна ненульова Velocity
velocity_avg = F(SPRINT1["completed"] + SPRINT2["completed"], 2)
assert velocity_avg == F(21, 2)             # = «Средн.: 10.5» у Velocity Chart

# ---- 3. Прогноз тривалості ------------------------------------------------------
sprints_total_work = ceil(F(backlog, velocity))
assert sprints_total_work == 3              # 53 / 21 = 2.52 → 3
remaining_after_s2 = backlog - SPRINT1["completed"] - SPRINT2["completed"]
assert remaining_after_s2 == 32
sprints_left = ceil(F(remaining_after_s2, velocity))
assert sprints_left == 2                    # Sprint 3, Sprint 4
mvp_left = MUST + SHOULD - SPRINT2["completed"]
assert mvp_left == 19 and ceil(F(mvp_left, velocity)) == 1
sprint3_end = SPRINT2_START + timedelta(days=SPRINT_DAYS)        # Sprint 3 стартував 05.11 після закриття Sprint 2
sprint4_end = sprint3_end + timedelta(days=SPRINT_DAYS)
assert sprint3_end == date(2026, 11, 19) and sprint4_end == date(2026, 12, 3)
# Чутливість: середня Velocity 10.5
sens_left = ceil(F(remaining_after_s2, 1) / velocity_avg)
sens_mvp = ceil(F(mvp_left, 1) / velocity_avg)
assert (sens_left, sens_mvp) == (4, 2)

# ---- 4. Вартість команди ----------------------------------------------------------
team_cost = TEAM * HOURS_PER_SPRINT * RATE
assert team_cost == 600
cost_per_sp = F(team_cost, velocity)        # 600 / 21
assert round(float(cost_per_sp), 2) == 28.57
assert safe_div(team_cost, SPRINT1["completed"]) is None    # за Velocity Sprint 1 — ділення на 0

# ---- 5. Вартість по Epics -------------------------------------------------------
epic_cost = {key: sp * cost_per_sp for key, sp in EPIC_SP.items()}
estimate = sum(epic_cost.values())
assert estimate == backlog * cost_per_sp

# ---- 6. Бюджет ----------------------------------------------------------------------
contingency = estimate * CONTINGENCY
working = estimate + contingency
management = working * MANAGEMENT
total = working + management
assert round(float(total), 2) == 1915.57

# ---- 7. Earned Value ------------------------------------------------------------
def evm(committed_sp, completed_sp, spent_min):
    pv = committed_sp * cost_per_sp
    ev = completed_sp * cost_per_sp
    ac = F(spent_min, 60) * RATE * TEAM
    cpi = safe_div(ev, ac)
    spi = safe_div(ev, pv)
    eac = None if not cpi else total / cpi
    return {"PV": pv, "EV": ev, "AC": ac, "CV": ev - ac, "SV": ev - pv, "CPI": cpi, "SPI": spi, "EAC": eac}


s1 = evm(SPRINT1["committed"], SPRINT1["completed"], SPRINT1["time_spent_min"])
s2 = evm(SPRINT2["committed"], SPRINT2["completed"], SPRINT2["time_spent_min"])
cum = evm(SPRINT1["committed"] + SPRINT2["committed"], SPRINT1["completed"] + SPRINT2["completed"],
          SPRINT1["time_spent_min"] + SPRINT2["time_spent_min"])
assert s1["CPI"] is None and s1["SPI"] == 0 and s1["EAC"] is None   # 0/0 — не визначено
assert s2["SPI"] == 1 and s2["AC"] == F(17, 4)                        # 17 хв × $15/год = $4.25
# Гіпотетичний сценарій (НЕ факт): Sprint 2 виконує людина за плановими 42 год
human_ac = 42 * RATE
human_cpi = s2["EV"] / human_ac
assert round(float(human_cpi), 3) == 0.952


def fmt(value):
    if value is None:
        return "не визначено"
    return f"{float(value):.4f}".rstrip("0").rstrip(".")


if __name__ == "__main__":
    print(f"Backlog {backlog} SP; h/SP {hours_per_sp}; Velocity S1 {SPRINT1['completed']}, S2 {velocity}, avg {float(velocity_avg)}")
    print(f"Sprints: total work {sprints_total_work}, left after S2 {sprints_left}; MVP {sprint3_end}, finish {sprint4_end}")
    print(f"Sensitivity (avg 10.5): left {sens_left} sprints, MVP {sens_mvp} sprints")
    print(f"Team cost {money(team_cost)}; cost/SP {money(cost_per_sp)}")
    for key, value in epic_cost.items():
        print(f"  {key}: {EPIC_SP[key]} SP -> {money(value)}")
    print(f"  IME-6 split: v1.0 3 SP {money(3 * cost_per_sp)}, v1.1 13 SP {money(13 * cost_per_sp)}")
    print(f"Estimate {money(estimate)}; CR {money(contingency)}; Working {money(working)}; MR {money(management)}; Total {money(total)}")
    for name, data in (("Sprint 1", s1), ("Sprint 2", s2), ("S1+S2", cum)):
        print(name, {k: (money(v) if k in ("PV", "EV", "AC", "CV", "SV", "EAC") and v is not None else fmt(v)) for k, v in data.items()})
    print(f"Hypothetical human Sprint 2: AC {money(human_ac)}, CPI {fmt(human_cpi)}")
    print("All assertions passed.")
