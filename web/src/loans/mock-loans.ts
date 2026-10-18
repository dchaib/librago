import { addDays, calendarDate, type Loan, type Option } from './loans.ts'

export const networks: Option[] = [{ id: 'rivages', name: 'Les Rivages' }, { id: 'clairiere', name: 'La Clairière' }]
export const borrowers: Option[] = [{ id: 'camille', name: 'Camille' }, { id: 'sacha', name: 'Sacha' }, { id: 'lou', name: 'Lou' }]

// Entirely fictional titles, authors, borrowers and networks. Dates are anchored
// once per page load so an open page really ages when the calendar day changes.
export function createMockLoans(today: string): Loan[] {
  const titles = [
    'La maison des marées', 'Un jardin sur la lune', 'Atlas des chemins oubliés',
    'Les oiseaux du dimanche', 'Le dernier phare', 'Petites recettes pour les grandes tablées',
    'Le voyage extraordinaire de la petite renarde qui voulait voir la mer et découvrir ce qui se cache derrière les montagnes',
    'Sous les étoiles', 'Les saisons du potager', 'La ville en papier', 'Carnets de voyage',
    'Le secret des pierres bleues', 'Dessiner la forêt', 'À vélo !', 'Histoires pour un soir de pluie',
    'L’île aux lucioles', 'Une année au grand air', 'Les aventures de Zoé — Tome 3 : Le passage des nuages',
    'La cuisine des jours heureux', 'Océans', 'Le musée des petites choses', 'Sur la piste du vent',
    'Mille et une façons de regarder le ciel', 'Le temps des cerises',
  ]
  const offsets = [-7, -3, -1, 0, 0, 1, 2, 2, 3, 4, 6, 7, 9, 10, 13, 14, 16, 18, 21, 21, 24, 28, 30, 35]
  const authors = ['Jeanne Morel', 'Émile Varenne', 'Nora Delcourt', 'Ariane Duval']
  return titles.map((title, index) => ({
    id: `demo-${index + 1}`, title,
    author: index % 5 === 0 ? undefined : authors[index % authors.length],
    cover: index % 5 === 2 ? undefined : `/covers/demo-${index % 4 + 1}.svg`,
    networkId: networks[index % 2].id,
    borrowerId: borrowers[index % 3].id,
    dueDate: addDays(today, offsets[index]),
  }))
}
const loadedAt = new Date()
export const mockLoans = createMockLoans(calendarDate(loadedAt))
export const mockSynchronizations = networks.map((network, index) => ({
  ...network, lastSuccess: new Date(loadedAt.getTime() - (index + 1) * 17 * 60000).toISOString(),
}))
