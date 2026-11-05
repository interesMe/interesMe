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
  const skillCalls: string[][] = [];

  async function render(): Promise<HTMLElement> {
    await TestBed.configureTestingModule({
      imports: [PeopleSearchPage],
      providers: [
        provideRouter([]),
        {
          provide: PeopleSearchApiService,
          useValue: {
            searchPeople: (interestIds: string[] = [], skills: string[] = []) => {
              searchCalls.push([...interestIds]);
              skillCalls.push([...skills]);
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
    skillCalls.length = 0;
  });

  it('renders matches with shared interests, skills and a profile link', async () => {
    response = {
      interestIds: ['i-1'],
      usedMyInterests: true,
      skillFilters: [],
      availableSkills: ['Figma', 'Angular'],
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
    response = { interestIds: ['i-1'], usedMyInterests: false, skillFilters: [], availableSkills: [], items: [] };

    const element = await render();

    expect(fixture.componentInstance.isEmpty()).toBe(true);
    expect(element.querySelector('.state-card')).not.toBeNull();
    expect(element.querySelector('.person-card')).toBeNull();
  });

  it('asks the user to add interests when they have none', async () => {
    response = { interestIds: [], usedMyInterests: true, skillFilters: [], availableSkills: [], items: [] };

    await render();

    expect(fixture.componentInstance.hasNoOwnInterests()).toBe(true);
    expect(fixture.componentInstance.isEmpty()).toBe(false);
  });

  // IME-12: skill filter chips.
  it('filters by selected skills and restores the initial search on reset', async () => {
    response = {
      interestIds: ['i-1'],
      usedMyInterests: true,
      skillFilters: [],
      availableSkills: ['Figma', 'Angular'],
      items: [],
    };

    const element = await render();
    const chips = Array.from(element.querySelectorAll<HTMLButtonElement>('.skill-option'));
    expect(chips.map((chip) => chip.textContent?.trim())).toEqual(['Figma', 'Angular']);

    fixture.componentInstance.toggleSkill('Figma');
    fixture.componentInstance.toggleSkill('Angular');
    expect(skillCalls.at(-1)).toEqual(['figma', 'angular']);
    expect(fixture.componentInstance.isSkillSelected('FIGMA')).toBe(true);

    fixture.componentInstance.toggleSkill('Figma');
    expect(skillCalls.at(-1)).toEqual(['angular']);

    fixture.componentInstance.resetFilters();
    expect(skillCalls.at(-1)).toEqual([]);
    expect(searchCalls.at(-1)).toEqual([]);
    expect(fixture.componentInstance.hasActiveFilters()).toBe(false);
  });
});
