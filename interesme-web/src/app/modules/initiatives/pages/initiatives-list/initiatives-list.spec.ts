import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';

import { Initiative, InitiativeGoalType, InitiativeListQuery } from '../../models';
import { InitiativesApiService } from '../../services/initiatives-api.service';
import { InitiativesListPage } from './initiatives-list';

// IME-14: list, search and empty / no-results states.
describe('InitiativesListPage', () => {
  let fixture: ComponentFixture<InitiativesListPage>;
  let results: Initiative[] = [];
  const queries: InitiativeListQuery[] = [];

  async function render(): Promise<HTMLElement> {
    await TestBed.configureTestingModule({
      imports: [InitiativesListPage],
      providers: [
        provideRouter([]),
        {
          provide: InitiativesApiService,
          useValue: {
            getAll: (query: InitiativeListQuery) => {
              queries.push(query);
              return of(results);
            },
          },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(InitiativesListPage);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
    return fixture.nativeElement as HTMLElement;
  }

  beforeEach(() => {
    results = [];
    queries.length = 0;
  });

  it('shows the empty state (create first) when there are no initiatives and no filters', async () => {
    const element = await render();

    expect(fixture.componentInstance.hasAppliedFilters()).toBe(false);
    expect(element.textContent).toContain('No initiatives yet');
  });

  it('sends search and goal filters and shows a no-results state', async () => {
    const element = await render();

    fixture.componentInstance.searchText.set('  robots ');
    fixture.componentInstance.goalFilter.set(InitiativeGoalType.Build);
    fixture.componentInstance.applyFilters();
    fixture.detectChanges();

    expect(queries.at(-1)).toEqual({ search: 'robots', goalType: InitiativeGoalType.Build });
    expect(fixture.componentInstance.hasAppliedFilters()).toBe(true);
    expect(element.textContent).toContain('Nothing found');

    fixture.componentInstance.clearFilters();
    expect(queries.at(-1)).toEqual({ search: '', goalType: null });
  });
});
