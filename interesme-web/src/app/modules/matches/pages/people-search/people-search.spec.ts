import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';

import { ProfileApiService } from '../../../profile/services/profile-api.service';
import { PeopleSearchResponse } from '../../models/people-search';
import { PeopleSearchApiService } from '../../services/people-search-api.service';
import { PeopleSearchPage } from './people-search';

// IME-11: people search page states.
describe('PeopleSearchPage', () => {
  let fixture: ComponentFixture<PeopleSearchPage>;
  let response: PeopleSearchResponse;
  const searchCalls: string[][] = [];

  async function render(): Promise<HTMLElement> {
    await TestBed.configureTestingModule({
      imports: [PeopleSearchPage],
      providers: [
        provideRouter([]),
        {
          provide: PeopleSearchApiService,
          useValue: {
            searchPeople: (interestIds: string[] = []) => {
              searchCalls.push([...interestIds]);
              return of(response);
            },
          },
        },
        {
          provide: ProfileApiService,
          useValue: { getInterests: () => of([{ id: 'i-1', name: 'Design' }]) },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(PeopleSearchPage);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    return fixture.nativeElement as HTMLElement;
  }

  beforeEach(() => {
    searchCalls.length = 0;
  });

  it('renders matches with shared interests, skills and a profile link', async () => {
    response = {
      interestIds: ['i-1'],
      usedMyInterests: true,
      items: [
        {
          userId: 'u-1',
          displayName: 'Anna Designer',
          headline: 'Product designer',
          city: 'Kyiv',
          avatarUrl: null,
          sharedInterestCount: 1,
          sharedInterests: [{ id: 'i-1', name: 'Design', color: '#f00' }],
          skills: ['Figma'],
        },
      ],
    };

    const element = await render();

    expect(searchCalls[0]).toEqual([]);
    expect(element.textContent).toContain('Anna Designer');
    expect(element.textContent).toContain('Design');
    expect(element.textContent).toContain('Figma');
    expect(element.querySelector('a[href="/u/u-1"]')).not.toBeNull();
  });

  it('shows an empty-state message when nobody matches', async () => {
    response = { interestIds: ['i-1'], usedMyInterests: false, items: [] };

    const element = await render();

    expect(fixture.componentInstance.isEmpty()).toBe(true);
    expect(element.querySelector('.state-card')).not.toBeNull();
    expect(element.querySelector('.person-card')).toBeNull();
  });

  it('asks the user to add interests when they have none', async () => {
    response = { interestIds: [], usedMyInterests: true, items: [] };

    await render();

    expect(fixture.componentInstance.hasNoOwnInterests()).toBe(true);
    expect(fixture.componentInstance.isEmpty()).toBe(false);
  });
});
