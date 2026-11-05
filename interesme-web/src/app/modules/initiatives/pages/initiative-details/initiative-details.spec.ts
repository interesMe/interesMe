import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter, Router } from '@angular/router';
import { of } from 'rxjs';

import { AuthService } from '../../../auth/services/auth.service';
import {
  Initiative,
  InitiativeGoalType,
  InitiativeJoinRequestStatus,
  InitiativeStatus,
  InitiativeVisibility,
} from '../../models';
import { InitiativesApiService } from '../../services/initiatives-api.service';
import { InitiativeDetailsPage } from './initiative-details';

function initiative(overrides: Partial<Initiative> = {}): Initiative {
  return {
    id: 'init-1',
    ownerUserId: 'owner-1',
    ownerDisplayName: 'Olena',
    slug: 'study-planner',
    title: 'Study planner',
    shortDescription: 'Planner for students',
    goalType: InitiativeGoalType.Build,
    university: 'NUBiP',
    teamSize: 3,
    status: InitiativeStatus.Active,
    visibility: InitiativeVisibility.Public,
    createdAt: '2026-11-05T10:00:00Z',
    updatedAt: '2026-11-05T10:00:00Z',
    interests: [{ id: 'i-1', name: 'Startups', slug: 'startups' }],
    roles: [{ id: 'r-1', name: 'Designer' }],
    members: [],
    memberCount: 0,
    myJoinRequest: null,
    ...overrides,
  };
}

// IME-14 / IME-15: details page states.
describe('InitiativeDetailsPage', () => {
  let fixture: ComponentFixture<InitiativeDetailsPage>;
  let currentUserId = 'viewer-1';
  const deleteCalls: string[] = [];

  async function render(item: Initiative): Promise<HTMLElement> {
    await TestBed.configureTestingModule({
      imports: [InitiativeDetailsPage],
      providers: [
        provideRouter([]),
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: convertToParamMap({ id: item.id }) } } },
        {
          provide: InitiativesApiService,
          useValue: {
            getById: () => of(item),
            delete: (id: string) => {
              deleteCalls.push(id);
              return of(undefined);
            },
          },
        },
        { provide: AuthService, useValue: { user: () => ({ id: currentUserId }) } },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(InitiativeDetailsPage);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
    return fixture.nativeElement as HTMLElement;
  }

  beforeEach(() => {
    currentUserId = 'viewer-1';
    deleteCalls.length = 0;
  });

  it('shows owner, requirements and current team with fill count', async () => {
    const element = await render(
      initiative({
        members: [{ userId: 'm-1', displayName: 'Taras', avatarUrl: null, roleName: 'Designer', joinedAt: '' }],
        memberCount: 1,
      }),
    );

    expect(element.textContent).toContain('Olena');
    expect(element.textContent).toContain('Taras');
    expect(element.textContent).toContain('Designer');
    expect(fixture.componentInstance.teamCount()).toBe(2);
    expect(element.querySelector('a[href="/initiatives/init-1/apply"]')).not.toBeNull();
  });

  it('replaces the apply button with the pending status', async () => {
    const element = await render(
      initiative({
        myJoinRequest: {
          id: 'jr-1',
          status: InitiativeJoinRequestStatus.Pending,
          roleId: null,
          createdAt: '',
          updatedAt: '',
        },
      }),
    );

    expect(fixture.componentInstance.canApply()).toBe(false);
    expect(element.querySelector('.request-status.is-pending')).not.toBeNull();
    expect(element.querySelector('a[href="/initiatives/init-1/apply"]')).toBeNull();
  });

  it('allows re-applying after a rejection but not when the team is full or closed', async () => {
    await render(
      initiative({
        myJoinRequest: {
          id: 'jr-1',
          status: InitiativeJoinRequestStatus.Rejected,
          roleId: null,
          createdAt: '',
          updatedAt: '',
        },
      }),
    );
    expect(fixture.componentInstance.canApply()).toBe(true);

    fixture.componentInstance.initiative.set(initiative({ teamSize: 2, memberCount: 1 }));
    expect(fixture.componentInstance.isTeamFull()).toBe(true);
    expect(fixture.componentInstance.canApply()).toBe(false);

    fixture.componentInstance.initiative.set(initiative({ status: InitiativeStatus.Archived }));
    expect(fixture.componentInstance.canApply()).toBe(false);
  });

  it('lets the owner delete only after an explicit confirmation', async () => {
    currentUserId = 'owner-1';
    const element = await render(initiative());
    const router = TestBed.inject(Router);
    const navigate = vi.spyOn(router, 'navigateByUrl').mockResolvedValue(true);

    expect(fixture.componentInstance.isOwner()).toBe(true);
    expect(element.querySelector('a[href="/initiatives/init-1/apply"]')).toBeNull();

    fixture.componentInstance.askDelete();
    expect(deleteCalls).toEqual([]);

    fixture.componentInstance.confirmDelete(fixture.componentInstance.initiative()!);
    expect(deleteCalls).toEqual(['init-1']);
    expect(navigate).toHaveBeenCalledWith('/me/initiatives');
  });
});
