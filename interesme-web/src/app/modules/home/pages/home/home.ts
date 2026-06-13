import { Component, computed, inject } from '@angular/core';

import { I18nService } from '../../../../core/i18n/i18n.service';
import { TranslationKey } from '../../../../core/i18n/translations';

type Initiative = {
  titleKey: TranslationKey;
  descriptionKey: TranslationKey;
  tagKeys: TranslationKey[];
  members: string;
  roles: number;
  tone: 'startup' | 'band' | 'garden';
};

type Message = {
  nameKey: TranslationKey;
  textKey: TranslationKey;
  time: string;
  initials: string;
};

@Component({
  selector: 'app-home-page',
  standalone: true,
  templateUrl: './home.html',
  styleUrl: './home.scss',
})
export class HomePage {
  private readonly i18n = inject(I18nService);

  readonly text = computed(() => {
    this.i18n.currentLocale();

    return {
      welcomeBack: this.i18n.t('home.hero.eyebrow'),
      create: this.i18n.t('home.hero.create'),
      explore: this.i18n.t('home.hero.explore'),
      description: this.i18n.t('home.hero.description'),
      searchPlaceholder: this.i18n.t('home.search.placeholder'),
      createInitiativeTitle: this.i18n.t('home.actions.create.title'),
      createInitiativeDescription: this.i18n.t('home.actions.create.description'),
      createInitiativeButton: this.i18n.t('home.actions.create.button'),
      exploreInitiativesTitle: this.i18n.t('home.actions.explore.title'),
      exploreInitiativesDescription: this.i18n.t('home.actions.explore.description'),
      exploreInitiativesButton: this.i18n.t('home.actions.explore.button'),
      liveOpportunities: this.i18n.t('home.featured.eyebrow'),
      featuredInitiatives: this.i18n.t('home.featured.title'),
      viewAll: this.i18n.t('home.common.viewAll'),
      members: this.i18n.t('home.card.members'),
      openRoles: this.i18n.t('home.card.openRoles'),
      join: this.i18n.t('home.card.join'),
      missionTitle: this.i18n.t('home.mission.title'),
      missionDescription: this.i18n.t('home.mission.description'),
      messages: this.i18n.t('home.messages.title'),
      trending: this.i18n.t('home.trending.title'),
      active: this.i18n.t('home.trending.active'),
    };
  });

  private readonly initiativeItems: Initiative[] = [
    {
      titleKey: 'home.initiatives.startup.title',
      descriptionKey: 'home.initiatives.startup.description',
      tagKeys: ['home.tags.startup', 'home.tags.students', 'home.tags.teamwork'],
      members: '4/8',
      roles: 2,
      tone: 'startup',
    },
    {
      titleKey: 'home.initiatives.band.title',
      descriptionKey: 'home.initiatives.band.description',
      tagKeys: ['home.tags.music', 'home.tags.creative', 'home.tags.band'],
      members: '2/5',
      roles: 3,
      tone: 'band',
    },
    {
      titleKey: 'home.initiatives.garden.title',
      descriptionKey: 'home.initiatives.garden.description',
      tagKeys: ['home.tags.community', 'home.tags.nature', 'home.tags.local'],
      members: '6/12',
      roles: 4,
      tone: 'garden',
    },
  ];

  readonly initiatives = computed(() => {
    this.i18n.currentLocale();

    return this.initiativeItems.map((initiative) => ({
      ...initiative,
      title: this.i18n.t(initiative.titleKey),
      description: this.i18n.t(initiative.descriptionKey),
      tags: initiative.tagKeys.map((tagKey) => this.i18n.t(tagKey)),
    }));
  });

  private readonly messageItems: Message[] = [
    {
      nameKey: 'home.messages.alex.name',
      textKey: 'home.messages.alex.text',
      time: '2m',
      initials: 'AJ',
    },
    {
      nameKey: 'home.messages.sarah.name',
      textKey: 'home.messages.sarah.text',
      time: '1h',
      initials: 'SC',
    },
    {
      nameKey: 'home.messages.mike.name',
      textKey: 'home.messages.mike.text',
      time: '3h',
      initials: 'MR',
    },
  ];

  readonly messages = computed(() => {
    this.i18n.currentLocale();

    return this.messageItems.map((message) => ({
      ...message,
      name: this.i18n.t(message.nameKey),
      text: this.i18n.t(message.textKey),
    }));
  });

  readonly trending = computed(() => {
    this.i18n.currentLocale();

    return [
      this.i18n.t('home.trending.startup'),
      this.i18n.t('home.trending.band'),
      this.i18n.t('home.trending.gameJam'),
      this.i18n.t('home.trending.garden'),
    ];
  });
}
